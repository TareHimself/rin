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
using Rin.World.Actors;
using Rin.World.Components;
using Rin.World.Components.Lights;
using Rin.World.Graphics;
using Rin.World.Graphics.Default;
using Rin.World.Graphics.Mesh;
using Rin.World.Mesh;
using Rin.World.Physics.Bepu;
using SharpGLTF.Schema2;
using Texture = SharpGLTF.Schema2.Texture;

namespace Examples.Sponza;

public sealed class SponzaExample : Example
{
    private static readonly Log Log = Log.For<SponzaExample>();

    private readonly Lock _lock = new();
    private IMeshFactory? _meshFactory;
    private World? _world;
    private ResourceHandle[] _textures = [];

    public override string Name => "sponza";

    public override string Title => "Sponza";

    public override Extent2D WindowSize => new(500);

    public override void Start(ExampleContext context)
    {
        _meshFactory = Global.Provider.AddSingle<IMeshFactory>(new MeshFactory());

        Task.Run(() =>
        {
            LoadSponza(Path.Join(Global.Directory, "assets", "models", "sponza.glb"))
                .DispatchMain(loaded =>
                {
                    var world = loaded.World;
                    if (!context.IsActive)
                    {
                        world.Stop();
                        IGraphicsModule.Get().FreeResourceHandles(loaded.Textures);
                        return;
                    }

                    _world = world;
                    _textures = loaded.Textures;
                    var camera = world.AddActor<CameraActor>();
                    var light = camera.AddComponent<PointLightComponent>();
                    
                    light.Radiance = 100000;

                    context.Add(new TestViewport(camera));
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
        });
    }

    public override void Stop()
    {
        _world?.Stop();
        _world = null;
        IGraphicsModule.Get().FreeResourceHandles(_textures);
        _textures = [];
        _meshFactory?.Dispose();
        _meshFactory = null;
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
                var handle = image.CreateTexture(out var newHandle, sampler.MagFilter switch
                {
                    TextureInterpolationFilter.NEAREST => ImageFilter.Nearest,
                    TextureInterpolationFilter.LINEAR => ImageFilter.Linear,
                    TextureInterpolationFilter.DEFAULT => ImageFilter.Linear,
                    _ => throw new ArgumentOutOfRangeException()
                }, mips: true).GetAwaiter().GetResult();
                cache.Add(id, newHandle);
                return newHandle;
            }
        }
    }

    private sealed record LoadedScene(World World, ResourceHandle[] Textures);

    private async Task<LoadedScene> LoadSponza(string filename)
    {
        var model = ModelRoot.Load(filename);
        var mesh = model?.LogicalMeshes?.FirstOrDefault() ?? throw new NullReferenceException();
        IMeshMaterial?[] materials = new SponzaMeshMaterial?[mesh.Primitives.Count];
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
            materials[idx] = new SponzaMeshMaterial
            {
                ColorImageId = baseColorImage,
                NormalImageId = normalImage,
                MetallicRoughnessImageId = pbrImage
            };
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

        var task = IMeshFactory.Get()
            .CreateMesh(out var id, vertices.ToBuffer(), indices.ToBuffer(), surfaces.ToArray());
        await task;

        var world = new World(new DefaultRenderSystem(), new BepuPhysicsSystem());
        world.Start();

        world.AddActor(new Actor
        {
            RootComponent = new StaticMeshComponent
            {
                Materials = materials,
                Mesh = new StaticMesh
                {
                    MeshId = id
                }
            }
        });

        var sceneCenter = (sceneBounds.Min + sceneBounds.Max) / 2f;
        var sceneHeight = sceneBounds.Max.Y - sceneBounds.Min.Y;
        var sceneWidth = sceneBounds.Max.X - sceneBounds.Min.X;

        // Offset horizontally - straight down is parallel to Up, degenerate for LookTowards.
        var sunPosition = new Vector3(sceneCenter.X - sceneWidth * 0.4f, sceneBounds.Max.Y + sceneHeight,
            sceneCenter.Z);
        var sunDirection = Vector3.Normalize(sceneCenter - sunPosition);
        // world.AddActor(new Actor
        // {
        //     RootComponent = new DirectionalLightComponent
        //     {
        //         Radiance = 4,
        //         Rotation = MathR.LookTowards(sunPosition, sunDirection, MathR.Up)
        //     }
        // });

        // // Interior fill lights; Radiance is scaled up for inverse-square falloff.
        // var fillHeight = sceneBounds.Min.Y + sceneHeight * 0.6f;
        // foreach (var xFraction in new[] { 0.2f, 0.5f, 0.8f })
        //     world.AddActor(new Actor
        //     {
        //         RootComponent = new PointLightComponent
        //         {
        //             Location = new Vector3(sceneBounds.Min.X + sceneWidth * xFraction, fillHeight, sceneCenter.Z),
        //             Radiance = 800
        //         }
        //     });

        return new LoadedScene(world, textures.Values.ToArray());
    }
}
