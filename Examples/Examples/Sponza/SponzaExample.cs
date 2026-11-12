using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Examples.Common;
using Examples.Common.Views;
using Rin.Core;
using Rin.Core.Extensions;
using Rin.Core.Graphics;
using Rin.Core.Shared.Math;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Layouts;
using Rin.World;
using Rin.World.Graphics;
using Rin.World.Transforms;
using Rin.World.Graphics.Default;
using Rin.World.Graphics.Mesh;
using Rin.World.Graphics.Surfaces;
using SharpGLTF.Schema2;
using Camera = Rin.World.Graphics.Camera;
using Texture = SharpGLTF.Schema2.Texture;

namespace Examples.Sponza;

public sealed class SponzaExample : Example
{
    private static readonly Log Log = Log.For<SponzaExample>();

    private readonly Lock _lock = new();
    private IMeshFactory? _meshFactory;
    private RenderFeature? _render;
    private ResourceHandle[] _textures = [];

    public override string Name => "sponza";

    public override string Title => "Sponza";

    public override Extent2D WindowSize => new(500);

    public override void Start(ExampleContext context)
    {
        _meshFactory = Global.Provider.AddSingle<IMeshFactory>(new MeshFactory());

        Task.Run(() => LoadSponza(Path.Join(Global.Directory, "assets", "models", "sponza.glb")))
            .DispatchMain(loaded =>
            {
                var render = loaded.Render;
                if (!context.IsActive)
                {
                    render.RenderSystem.Dispose();
                    IGraphicsModule.Get().FreeResourceHandles(loaded.Textures);
                    return;
                }

                _render = render;
                _textures = loaded.Textures;
                var world = render.World;
                var camera = SpawnCamera(world);

                context.Add(new TestViewport(render, camera));
                context.Add(new PanelView
                {
                    InitSlots =
                    [
                        new PanelSlot
                        {
                            Child = new FpsView(),
                            MinAnchor = new Vector2(1f, 0f),
                            MaxAnchor = new Vector2(1f, 0f),
                            Alignment = new Vector2(1f, 0f),
                            SizeToContent = true
                        }
                    ]
                });
                context.OnUpdate(world.Update);
            });
    }

    public override void Stop()
    {
        _render?.RenderSystem.Dispose();
        _render = null;
        IGraphicsModule.Get().FreeResourceHandles(_textures);
        _textures = [];
        _meshFactory?.Dispose();
        _meshFactory = null;
    }

    private static Entity SpawnCamera(World world)
    {
        var camera = world.Spawn();
        world.SetPosition(camera, new Vector3(0, 100, 0));
        world.Add(camera, new Camera());

        foreach (var radiance in new[] { 200.0f, 100000.0f })
        {
            var light = world.Spawn();
            world.SetPosition(light, Vector3.Zero);
            world.SetParent(light, camera);
            world.Add(light, new PointLight { Radiance = radiance });
        }

        return camera;
    }

    private ResourceHandle LoadImage(Texture? texture, Dictionary<int, ResourceHandle> cache)
    {
        if (texture == null) return ResourceHandle.InvalidTexture;
        var tex = texture.PrimaryImage;
        var sampler = texture.Sampler;
        Debug.Assert(tex != null && sampler != null);
        lock (_lock)
        {
            var id = tex.Content.Content.GetHashCode();
            {
                if (cache.TryGetValue(id, out var image)) return image;
            }

            {
                using var image = HostImage.Create(new MemoryStream(tex.Content.Content.ToArray()));
                var newHandle = image.CreateTexture(mips: true);
                cache.Add(id, newHandle);
                return newHandle;
            }
        }
    }

    private sealed record LoadedScene(RenderFeature Render, ResourceHandle[] Textures);

    private LoadedScene LoadSponza(string filename)
    {
        var model = ModelRoot.Load(filename);
        var mesh = model?.LogicalMeshes?.FirstOrDefault() ?? throw new NullReferenceException();
        IMeshMaterial?[] materials = new IMeshMaterial?[mesh.Primitives.Count];
        List<MeshSurface> surfaces = [];
        List<uint> indices = [];
        List<Vertex> vertices = [];

        var textures = new Dictionary<int, ResourceHandle>();

        Log.Info("Loading textures");
        mesh.Primitives.Select((c, idx) => (c, idx)).AsParallel().ForAll(data =>
        {
            var (primitive, idx) = data;
            var material = primitive.Material;
            var baseColor = material.FindChannel("BaseColor") ?? throw new NullReferenceException();
            var normalColor = material.FindChannel("Normal") ?? throw new NullReferenceException();
            var pbrColor = material.FindChannel("MetallicRoughness") ?? throw new NullReferenceException();
            var baseColorImage = LoadImage(baseColor.Texture, textures);
            var normalImage = LoadImage(normalColor.Texture, textures);
            var pbrImage = LoadImage(pbrColor.Texture, textures);
            materials[idx] = new SurfaceMaterial<SponzaMaterial>(new SponzaMaterial
            {
                ColorTexture = baseColorImage,
                NormalTexture = normalImage,
                MetallicRoughnessTexture = pbrImage
            });
        });
        Log.Info("Loading geometry");
        foreach (var primitive in mesh.Primitives)
        {
            if (primitive == null) continue;

            List<Vertex> surfaceVertices = [];
            var newSurface = new MeshSurface
            {
                VertexStart = (uint)vertices.Count,
                VertexCount = (uint)primitive.VertexAccessors.First().Value.Count,
                IndicesStart = (uint)indices.Count,
                IndicesCount = (uint)primitive.IndexAccessor.Count
            };

            var initialVertex = vertices.Count;

            {
                foreach (var idx in primitive.GetIndices()) indices.Add(idx - (uint)initialVertex);
            }

            {
                foreach (var (position, normal, uv) in primitive.GetVertices("POSITION")
                             .AsVector3Array()
                             .Zip(
                                 primitive.GetVertices("NORMAL").AsVector3Array(),
                                 primitive.GetVertices("TEXCOORD_0").AsVector2Array()
                             ))
                    surfaceVertices.Add(new Vertex
                    {
                        Location = position,
                        Normal = normal,
                        UV = uv
                    });
            }

            newSurface.Bounds = CollectionsMarshal.AsSpan(surfaceVertices).ComputeBounds();
            vertices.AddRange(surfaceVertices);
            surfaces.Add(newSurface);
        }

        Log.Info("Geometry loaded");

        var sceneBounds = CollectionsMarshal.AsSpan(vertices).ComputeBounds();

        var meshFactory = IMeshFactory.Get();
        var sponza = meshFactory.CreateMesh<Vertex>(CollectionsMarshal.AsSpan(vertices),
            CollectionsMarshal.AsSpan(indices), surfaces.ToArray());

        var world = new World();
        var render = new RenderFeature(world, new DefaultRenderSystem(), meshFactory);
        world.Add(world.Spawn(), new StaticMesh { Mesh = sponza, Materials = render.Materials.Add(materials) });

        var sceneCenter = (sceneBounds.Min + sceneBounds.Max) / 2f;
        var sceneHeight = sceneBounds.Max.Y - sceneBounds.Min.Y;
        var sceneWidth = sceneBounds.Max.X - sceneBounds.Min.X;

        // Offset horizontally - straight down is parallel to Up, degenerate for LookTowards.
        var sunPosition = new Vector3(sceneCenter.X - sceneWidth * 0.4f, sceneBounds.Max.Y + sceneHeight,
            sceneCenter.Z);
        var sunDirection = Vector3.Normalize(sceneCenter - sunPosition);
        // var sun = world.Spawn();
        // world.SetRotation(sun, MathR.LookTowards(sunPosition, sunDirection, MathR.Up));
        // world.Add(sun, new DirectionalLight { Radiance = 4 });

        // // Interior fill lights; Radiance is scaled up for inverse-square falloff.
        // var fillHeight = sceneBounds.Min.Y + sceneHeight * 0.6f;
        // foreach (var xFraction in new[] { 0.2f, 0.5f, 0.8f })
        // {
        //     var fill = world.Spawn();
        //     world.SetPosition(fill, new Vector3(sceneBounds.Min.X + sceneWidth * xFraction, fillHeight, sceneCenter.Z));
        //     world.Add(fill, new PointLight { Radiance = 800 });
        // }

        return new LoadedScene(render, textures.Values.ToArray());
    }
}
