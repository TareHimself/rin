using System.Numerics;
using Examples.Common;
using Rin.Core;
using Rin.Core.Extensions;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Windows;
using Rin.Core.Shared.Math;
using Rin.GLTF;
using Rin.World;
using Rin.World.Actors;
using Rin.World.Components;
using Rin.World.Components.Lights;
using Rin.World.Graphics.Default;
using Rin.World.Graphics.Mesh;
using Rin.World.Math;
using Rin.World.Mesh.Skinning.Animation;
using Rin.World.Physics.Bepu;
using Rin.World.Views;
using Rin.Core.Views;

namespace AssetViewer;

/// <summary>
///     Loads one skinned asset, frames it automatically, and plays an animation clip - a fully
///     scripted way to visually check the import/skinning/animation pipeline without hand-driving
///     a camera. No args yet: model path, clip name, and camera framing are all hardcoded below;
///     extend as needed rather than building this out speculatively.
/// </summary>
public class AssetViewerApplication : ExampleApplication
{
    private const string ModelPath = "assets/models/fox.glb";
    private const string ClipName = "Run";

    private World? _scene;
    private CameraComponent? _camera;

    protected override void OnStartup()
    {
        IViewsModule.Get().OnSurfaceCreated += surf =>
        {
            var scene = _scene = new World(new DefaultRenderSystem(), new BepuPhysicsSystem());
            scene.Start();

            var camera = _camera = new CameraComponent { FarClipPlane = 1000f };
            scene.AddActor(new Actor { RootComponent = camera });

            scene.AddActor(new Actor
            {
                RootComponent = new DirectionalLightComponent
                {
                    Radiance = 8.0f,
                    Location = new Vector3(0f, 50f, 0f)
                }
            });
            scene.AddActor(new Actor
            {
                RootComponent = new PointLightComponent { Location = new Vector3(0f, 20f, -20f), Radiance = 800f }
            });

            LoadAndSpawnModel(scene, camera);

            var window = surf.Renderer.GetWindow();
            window.OnClose += _ => RequestExit();

            surf.Add(new Viewport(camera));
        };

        IGraphicsModule.Get()
            .CreateWindow("Rin Asset Viewer", new Extent2D(1280, 800), WindowFlags.Visible | WindowFlags.Resizable);

        OnUpdate += dt => _scene?.Update(dt);
    }

    protected override void OnShutdown()
    {
    }

    private static async void LoadAndSpawnModel(World scene, CameraComponent camera)
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
            scene.AddActor(new Actor
            {
                RootComponent = new SkinnedMeshComponent { Mesh = mesh, PoseSource = graph }
            });

            var bounds = IMeshFactory.Get().GetMesh(mesh.MeshId)!.GetBounds();
            var center = (bounds.Min + bounds.Max) / 2f;
            var radius = (bounds.Max - bounds.Min).Length() / 2f;
            var distance = float.Max(radius * 2.5f, 1f);

            var cameraPos = center + new Vector3(0f, radius * 0.5f, -distance);
            camera.SetLocation(cameraPos, Space.World);
            camera.SetRotation(MathR.LookTowards(center - cameraPos), Space.World);
        });
    }
}
