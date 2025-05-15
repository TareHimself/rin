using System.Diagnostics;
using System.Numerics;
using Rin.Engine;
using Rin.Engine.Extensions;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.Meshes;
using Rin.Engine.Graphics.Textures;
using Rin.Engine.Math;
using Rin.Engine.Views;
using Rin.Engine.Views.Composite;
using Rin.Engine.Views.Layouts;
using Rin.Engine.World;
using Rin.Engine.World.Actors;
using Rin.Engine.World.Components;
using Rin.Engine.World.Components.Lights;
using Rin.Engine.World.Graphics;
using Rin.Engine.World.Mesh;
using rin.Examples.Common.Views;
using Rin.Sources;
using SharpGLTF.Schema2;
using Texture = SharpGLTF.Schema2.Texture;

namespace Sponza;

[Module(typeof(SWorldModule))]
[AlwaysLoad]
public class MainModule : IModule
{
    private readonly Lock _lock = new();

    public void Start(SEngine engine)
    {
        SEngine.Get().Sources.AddSource(
            new ResourcesSource(typeof(MainModule).Assembly, "Sponza", ".Content."));
        SViewsModule.Get().OnSurfaceCreated += surf =>
        {
            Task.Run(() =>
            {
                LoadSponza(@"Sponza/sponza.glb")
                    .After(world =>
                    {
                        SEngine.Get().DispatchMain(() =>
                        {
                            var camera = world.AddActor<CameraActor>();
                            surf.Add(new TestViewport(camera));
                            surf.Add(new Panel()
                            {
                                Slots =
                                [
                                    new PanelSlot
                                    {
                                        Child = new FpsView(),
                                        MinAnchor = new Vector2(1f, 0f),
                                        MaxAnchor = new Vector2(1f, 0f),
                                        Alignment = new Vector2(1f,0f),
                                        SizeToContent = true
                                    }
                                ]
                            });
                        });
                    });
            });
        };

        var window = SGraphicsModule.Get().CreateWindow(500, 500, "Sponza");

        window.OnCloseRequested += _ =>
        {
            if (window.Parent != null)
                window.Dispose();
            else
                SEngine.Get().RequestExit();
        };
    }

    public void Stop(SEngine engine)
    {
    }

    private ImageHandle LoadImage(Texture? texture, Dictionary<int, ImageHandle> cache)
    {
        if (texture == null) return ImageHandle.InvalidImage;
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
                var handle = image.CreateTexture(sampler.MagFilter switch
                {
                    TextureInterpolationFilter.NEAREST => ImageFilter.Nearest,
                    TextureInterpolationFilter.LINEAR => ImageFilter.Linear,
                    TextureInterpolationFilter.DEFAULT => ImageFilter.Linear,
                    _ => throw new ArgumentOutOfRangeException()
                }, mips: true).handle;
                cache.Add(id, handle);
                return handle;
            }
        }
    }

    public async Task<World> LoadSponza(string filename)
    {

        var model = ModelRoot.Load(filename,ReadContext.Create((f) => SEngine.Get().Sources.Read(filename).ReadAll()));
        var mesh = model?.LogicalMeshes?.FirstOrDefault() ?? throw new NullReferenceException();
        IMeshMaterial?[] materials = new SponzaMeshMaterial?[mesh.Primitives.Count];
        List<MeshSurface> surfaces = [];
        List<uint> indices = [];
        List<Vertex> vertices = [];

        var textures = new Dictionary<int, ImageHandle>();

        Console.WriteLine("Loading Textures...");
        mesh.Primitives.Select((c, idx) => (c, idx)).AsParallel().ForAll(data =>
        {
            var (primitive, idx) = data;
            var material = primitive.Material;
            material.InitializePBRMetallicRoughness();
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
        Console.WriteLine("Done Loading Textures...");
        Console.WriteLine("Loading Geometry...");
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

            newSurface.Bounds = surfaceVertices.ComputeBounds();
            vertices.AddRange(surfaceVertices);
            surfaces.Add(newSurface);
        }

        Console.WriteLine("Done Loading Geometry...");

        var (id, task) = SGraphicsModule.Get().GetMeshFactory()
            .CreateMesh(vertices.ToBuffer(), indices.ToBuffer(), surfaces.ToArray());
        await task;

        var world = new World();

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

        world.AddActor(new Actor
        {
            RootComponent = new DirectionalLightComponent
            {
                Radiance = 0,
                Rotation = Quaternion.Identity.AddYaw(45)
            }
        });

        return world;
    }
}