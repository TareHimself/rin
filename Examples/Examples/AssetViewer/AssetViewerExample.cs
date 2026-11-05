using System.Numerics;
using Examples.Common;
using Rin.Core;
using Rin.Core.Graphics.Windows;
using Rin.Core.Shared.Math;
using Rin.GLTF;
using Rin.World;
using Rin.World.Graphics;
using Rin.World.Transforms;
using Rin.World.Graphics.Default;
using Rin.World.Graphics.Mesh;
using Rin.World.Mesh.Skinning.Animation;
using Rin.World.Views;

namespace Examples.AssetViewer;

/// <summary>
///     Loads one skinned asset, frames it automatically, and plays an animation clip - a fully
///     scripted way to visually check the import/skinning/animation pipeline without hand-driving
///     a camera. No args yet: model path, clip name, and camera framing are all hardcoded below;
///     extend as needed rather than building this out speculatively.
/// </summary>
public sealed class AssetViewerExample : Example
{
    private const string ModelPath = "assets/models/fox.glb";
    private const string ClipName = "Run";

    private RenderFeature? _render;
    private IMeshFactory? _meshFactory;

    public override string Name => "asset-viewer";

    public override string Title => "Asset Viewer";

    public override void Start(ExampleContext context)
    {
        var meshFactory = _meshFactory = Global.Provider.AddSingle<IMeshFactory>(new MeshFactory());

        var world = new World();
        var render = _render = new RenderFeature(world, new DefaultRenderSystem(), meshFactory);

        var camera = world.Spawn();
        world.SetPosition(camera, Vector3.Zero);
        world.Add(camera, new Camera { FarClipPlane = 1000f });

        var sun = world.Spawn();
        world.SetPosition(sun, new Vector3(0f, 50f, 0f));
        world.Add(sun, new DirectionalLight { Radiance = 8.0f });

        var fill = world.Spawn();
        world.SetPosition(fill, new Vector3(0f, 20f, -20f));
        world.Add(fill, new PointLight { Radiance = 800f });

        LoadAndSpawnModel(render, camera);

        context.Add(new Viewport(render, camera));

        context.OnUpdate(world.Update);
    }

    public override void Stop()
    {
        _render?.RenderSystem.Dispose();
        _render = null;
        _meshFactory?.Dispose();
        _meshFactory = null;
    }

    private async void LoadAndSpawnModel(RenderFeature render, Entity camera)
    {
        var path = Path.Join(Global.Directory, ModelPath);
        var mesh = await GltfMeshImporter.LoadSkinnedMesh(path);
        if (mesh is null) return;

        var clips = GltfAnimationImporter.LoadAnimationClips(path);
        var graph = new AnimationGraph(mesh.Skeleton);
        if (clips.TryGetValue(ClipName, out var clip))
            graph.Root = new ClipPlayerNode(clip.Bind(mesh.Skeleton)) { Loop = true };

        IApplication.Get().MainDispatcher.Enqueue(() =>
        {
            if (_render != render) return;
            var deviceMesh = IMeshFactory.Get().GetMesh(mesh.MeshId)!;
            var model = render.World.Spawn();
            render.World.Add(model, new SkinnedMesh { MeshId = mesh.MeshId, PoseSource = render.PoseSources.Add(graph) });

            var bounds = deviceMesh.GetBounds();
            var center = (bounds.Min + bounds.Max) / 2f;
            var radius = (bounds.Max - bounds.Min).Length() / 2f;
            var distance = float.Max(radius * 2.5f, 1f);

            var cameraPos = center + new Vector3(-distance, radius * 0.5f, 0f);
            render.World.SetPosition(camera, cameraPos);
            render.World.SetRotation(camera, MathR.LookTowards(center - cameraPos));
        });
    }
}
