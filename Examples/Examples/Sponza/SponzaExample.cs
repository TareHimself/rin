using System.Diagnostics;
using System.Numerics;
using Examples.Common;
using Examples.Common.Views;
using Rin.Core;
using Rin.Core.Extensions;
using Rin.Core.Graphics;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Layouts;
using Rin.GLTF;
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
                var camera = loaded.StartCamera ?? SpawnCamera(world);

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
        world.SetPosition(camera, new Vector3(0, 0.8f, 0));
        world.Add(camera, new Camera { Ev100 = 3f });

        var light = world.Spawn();
        world.SetPosition(light, Vector3.Zero);
        world.SetParent(light, camera);
        world.Add(light, new PointLight { Lumens = 800f });

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

    private sealed record LoadedScene(RenderFeature Render, ResourceHandle[] Textures, Entity? StartCamera);

    private LoadedScene LoadSponza(string filename)
    {
        var model = ModelRoot.Load(filename);
        var textures = new Dictionary<int, ResourceHandle>();

        Log.Info("Loading textures");
        var materials = new IMeshMaterial?[model.LogicalMaterials.Count];
        Parallel.For(0, materials.Length, i => materials[i] = CreateMaterial(model.LogicalMaterials[i], textures));

        Log.Info("Loading scene");
        var render = new RenderFeature(new World(), new DefaultRenderSystem(), IMeshFactory.Get());
        var scene = GltfSceneImporter.Load(model, render,
            material => material is null ? null : materials[material.LogicalIndex]);
        foreach (var warning in scene.Warnings) Log.Warn(warning);

        return new LoadedScene(render, textures.Values.ToArray(), scene.StartCamera);
    }

    private IMeshMaterial CreateMaterial(Material material, Dictionary<int, ResourceHandle> textures)
    {
        var baseColor = material.FindChannel("BaseColor") ?? throw new NullReferenceException();
        var normal = material.FindChannel("Normal") ?? throw new NullReferenceException();
        var metallicRoughness = material.FindChannel("MetallicRoughness") ?? throw new NullReferenceException();
        return new SurfaceMaterial<SponzaMaterial>(new SponzaMaterial
        {
            ColorTexture = LoadImage(baseColor.Texture, textures),
            NormalTexture = LoadImage(normal.Texture, textures),
            MetallicRoughnessTexture = LoadImage(metallicRoughness.Texture, textures)
        });
    }
}
