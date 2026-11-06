using System.Numerics;
using Examples.Common;
using Examples.Common.Views;
using experiments.Docking.Model;
using experiments.Docking.Views;
using Rin.Core;
using Rin.Core.Extensions;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Windows;
using Rin.Core.Shared.Math;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Content;
using Rin.Core.Views.Events;
using Rin.Core.Views.Layouts;
using Rin.GLTF;
using Rin.World;
using Rin.World.Graphics;
using Rin.World.Physics;
using Rin.World.Scripting;
using Rin.World.Transforms;
using Rin.World.Graphics.Default;
using Rin.World.Graphics.Mesh;
using Rin.World.Graphics.Surfaces;
using Rin.World.Mesh;
using Rin.World.Mesh.Skinning.Animation;
using Rin.World.Physics.Bepu;
using Camera = Rin.World.Graphics.Camera;
using StaticMesh = Rin.World.Graphics.StaticMesh;

namespace Examples.SceneTest;

public sealed class SceneTestExample : Example
{
    private const int BenchBoxCount = 2000;
    private const int BenchGridSide = 25;
    private const float BenchGridSpacing = 5f;
    private const float BoxRainIntervalSeconds = 3f;

    private readonly Random _rng = new();
    private int? _cubeMeshId;
    private MaterialSet _boxMaterials;
    private RenderFeature? _render;
    private PhysicsFeature? _physics;
    private IMeshFactory? _meshFactory;
    private TextureCache _textures = null!;

    public override string Name => "scene-test";

    public override string Title => "Scene Test";

    public override void Start(ExampleContext context)
    {
        var meshFactory = _meshFactory = Global.Provider.AddSingle<IMeshFactory>(new MeshFactory());
        _textures = context.Application.Textures;
        var surf = context.Surface;

        var world = new World();
        var render = _render = new RenderFeature(world, new DefaultRenderSystem(), meshFactory);
        _physics = new PhysicsFeature(world, new BepuPhysicsSystem());
        var scripts = new ScriptFeature(world);

        // A perspective camera looking down +Z at the play area, and a top-down camera.
        var perspectiveCam = SpawnCamera(world, new Vector3(0f, 10f, -40f), Quaternion.Identity);
        var topCam = SpawnCamera(world, new Vector3(0f, 80f, 20f), MathR.LookTowards(new Vector3(0f, -1f, 0f)));

        SpawnBox(world, new Vector3(0f, -6f, 20f), new Vector3(200f, 1f, 200f), PhysicsState.Static, null);

        GltfMeshImporter.LoadStaticMesh(Path.Join(Global.Directory, "assets", "models", "cube.glb")).DispatchMain(mesh =>
        {
            if (_render != render || mesh is null) return;
            _cubeMeshId = mesh.MeshId;

            SpawnPointLight(world, new Vector3(0f, 35f, 20f));
            SpawnPointLight(world, new Vector3(0f, -20f, 0f));

            var sun = world.Spawn();
            var sunPosition = new Vector3(0.0f, 200.0f, 0.0f);
            world.SetPosition(sun, sunPosition);
            world.SetRotation(sun, MathR.LookTowards(Vector3.Normalize(Vector3.Zero - sunPosition)));
            world.Add(sun, new DirectionalLight { Radiance = 10.0f });

            // Static visible obstacle the dynamic boxes pile against.
            SpawnBox(world, new Vector3(0f, -2.5f, 22f), new Vector3(10f, 5f, 10f), PhysicsState.Static, mesh.MeshId);
            SpawnWavyPillar(render, mesh.MeshId);

            LoadGoldMaterial().DispatchMain(material =>
            {
                if (_render != render) return;
                _boxMaterials = render.Materials.Add(material);
                SpawnBoxes(30);
                scripts.Attach(world.Spawn(), new BoxRain(BoxRainIntervalSeconds, () => SpawnBoxes(1)));
                LoadFoxes(render, material);
            });
        });

        context.OnKey(e =>
        {
            if (e is { Key: InputKey.P, State: InputState.Pressed })
                IApplication.Get().MainDispatcher.Enqueue(() => SpawnBoxGrid(BenchBoxCount));
        });

        context.OnUpdate(world.Update);
        context.Add(BuildDockLayout(render, perspectiveCam, topCam));
        context.Add(RenderGraphOverlay.GraphOverlay.Create(surf));
    }

    public override void Stop()
    {
        _physics?.PhysicsSystem.Destroy();
        _physics = null;
        _render?.RenderSystem.Dispose();
        _render = null;
        _cubeMeshId = null;
        _boxMaterials = default;
        _meshFactory?.Dispose();
        _meshFactory = null;
    }

    private static Entity SpawnCamera(World world, Vector3 position, Quaternion rotation)
    {
        var camera = world.Spawn();
        world.SetPosition(camera, position);
        world.SetRotation(camera, rotation);
        world.Add(camera, new Camera());

        var light = world.Spawn();
        world.SetPosition(light, Vector3.Zero);
        world.SetParent(light, camera);
        world.Add(light, new PointLight { Radiance = 200.0f });
        return camera;
    }

    private static void SpawnPointLight(World world, Vector3 position)
    {
        var light = world.Spawn();
        world.SetPosition(light, position);
        world.Add(light, new PointLight { Radiance = 800.0f });
    }

    // The physics system multiplies the box size by the entity scale, as the old SceneTest did with the same values.
    private void SpawnBox(World world, Vector3 position, Vector3 size, PhysicsState state, int? meshId)
    {
        var box = world.Spawn();
        world.SetLocalTransform(box, new LocalTransform { Position = position, Scale = size });
        world.Add(box, new RigidBody { Shape = ColliderShape.Box(size), State = state });
        if (meshId is { } id) world.Add(box, new StaticMesh { MeshId = id, Materials = _boxMaterials });
    }

    private static void SpawnWavyPillar(RenderFeature render, int meshId)
    {
        var materials = render.Materials.Add(new SurfaceMaterial<WavyMaterial>(new WavyMaterial
        {
            Color = new Vector3(0.2f, 0.6f, 1f),
            Amplitude = 1.5f,
            Speed = 2f,
            BaseY = -6f,
            Height = 16f
        }));
        var pillar = render.World.Spawn();
        render.World.SetLocalTransform(pillar,
            new LocalTransform { Position = new Vector3(-15f, 2f, 22f), Scale = new Vector3(2f, 16f, 2f) });
        render.World.Add(pillar, new StaticMesh { MeshId = meshId, Materials = materials });
    }

    private void SpawnBoxes(int count)
    {
        if (_render?.World is not { } world || _cubeMeshId is not { } meshId) return;

        for (var i = 0; i < count; i++)
        {
            var position = new Vector3(
                (float)(_rng.NextDouble() * 10.0 - 5.0),
                26f + i * 3f,
                22f + (float)(_rng.NextDouble() * 10.0 - 5.0));
            SpawnBox(world, position, new Vector3(2f), PhysicsState.Simulated, meshId);
        }
    }

    // Fixed layout so every bench run starts from the same scene.
    private void SpawnBoxGrid(int count)
    {
        if (_render?.World is not { } world || _cubeMeshId is not { } meshId) return;

        const float halfExtent = (BenchGridSide - 1) * BenchGridSpacing * 0.5f;
        for (var i = 0; i < count; i++)
        {
            var layer = i / (BenchGridSide * BenchGridSide);
            var cell = i % (BenchGridSide * BenchGridSide);
            var position = new Vector3(
                cell % BenchGridSide * BenchGridSpacing - halfExtent,
                26f + layer * BenchGridSpacing,
                22f + cell / BenchGridSide * BenchGridSpacing - halfExtent);
            SpawnBox(world, position, new Vector3(2f), PhysicsState.Simulated, meshId);
        }
    }

    // The Khronos Fox sample is authored about 79 units tall, so it is scaled down to the size of the boxes.
    private void LoadFoxes(RenderFeature render, SurfaceMaterial<PbrMaterial> material)
    {
        var foxPath = Path.Join(Global.Directory, "assets", "models", "fox.glb");
        GltfMeshImporter.LoadSkinnedMesh(foxPath).DispatchMain(skinned =>
        {
            if (_render != render || skinned is null) return;
            if (!IMeshFactory.Get().IsMeshReady(skinned.MeshId)) return;

            var world = render.World;
            var materials = render.Materials.Add(material);
            var clips = GltfAnimationImporter.LoadAnimationClips(foxPath);
            foreach (var (x, clipName) in new[] { (-30f, "Survey"), (0f, "Walk"), (30f, "Run") })
            {
                var graph = new AnimationGraph(skinned.Skeleton);
                if (clips.TryGetValue(clipName, out var clip))
                    graph.Root = new ClipPlayerNode(clip.Bind(skinned.Skeleton)) { Loop = true };

                var fox = world.Spawn();
                world.SetPosition(fox, new Vector3(x, -5f, 55f));
                world.SetScale(fox, new Vector3(0.05f));
                world.Add(fox, new SkinnedMesh
                {
                    MeshId = skinned.MeshId,
                    Materials = materials,
                    PoseSource = render.PoseSources.Add(graph)
                });
            }
        });
    }

    private static DockSpaceView BuildDockLayout(RenderFeature render, Entity perspectiveCam, Entity topCam)
    {
        DockPanel Panel(string id, string title, IView content, Color? bg = null)
        {
            return new DockPanel(id, title, new RectView
            {
                Color = bg ?? Color.FromRgb(0.12f, 0.12f, 0.14f),
                Padding = new Padding(10f),
                InitChild = content
            });
        }

        var viewports = new DockSplitNode(DockOrientation.Vertical,
            new DockTabGroupNode(new DockPanel("perspective", "Perspective", new TestViewport(render, perspectiveCam))),
            new DockTabGroupNode(new DockPanel("top", "Top", new TestViewport(render, topCam))));

        var stats = new DockTabGroupNode(Panel("stats", "Stats", new FpsView(),
            Color.FromRgb(0.10f, 0.10f, 0.12f)));
        var controls = new DockTabGroupNode(Panel("controls", "Controls",
            new FlexBoxView(Axis.Column)
            {
                InitSlots =
                [
                    new FlexBoxSlot
                    {
                        Child = new TextBoxView
                        {
                            Content = "RMB drag: look\nWASD: move\nLMB: cycle view channel\nP: drop 2000 boxes",
                            FontSize = 14f,
                            WrapContent = true
                        }
                    },
                    new FlexBoxSlot { Child = BuildTimeScaleControl(render.World), Flex = 1, Fit = CrossFit.Fill }
                ]
            }));
        var side = new DockSplitNode(DockOrientation.Vertical, stats, controls);
        side.Weights[0] = 0.42f;
        side.Weights[1] = 0.58f;
        side.NormalizeWeights();

        var root = new DockSplitNode(DockOrientation.Horizontal, viewports, side);
        root.Weights[0] = 0.8f;
        root.Weights[1] = 0.2f;
        root.NormalizeWeights();

        return new DockSpaceView(new DockTree(root));
    }

    private const float MaxTimeScale = 3f;

    private static IView BuildTimeScaleControl(World scene)
    {
        var label = new LiveLabelView(() => $"TimeScale: {scene.TimeScale:F2}x") { FontSize = 14f };
        var slider = new SliderView(
            () => scene.TimeScale / MaxTimeScale,
            frac => scene.TimeScale = frac * MaxTimeScale)
        {
            BackgroundColor = Color.FromRgb(0.2f, 0.2f, 0.22f),
            ForegroundColor = Color.FromRgb(0.3f, 0.6f, 1f),
            Padding = new Padding { Top = 8f }
        };

        return new FlexBoxView(Axis.Column)
        {
            InitSlots =
            [
                new FlexBoxSlot { Child = label },
                new FlexBoxSlot { Child = slider, Fit = CrossFit.Fill }
            ]
        };
    }

    private sealed class LiveLabelView(Func<string> getText) : TextBoxView
    {
        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
            Content = getText();
        }
    }

    private sealed class SliderView(Func<float> getProgress, Action<float> onClick) : ProgressBarView(getProgress, onClick)
    {
        private const float Height = 28f;
        private bool _dragging;

        protected override Vector2 LayoutContent(in Vector2 availableSpace)
        {
            return base.LayoutContent(availableSpace with { Y = Height });
        }

        // Base ProgressBarView only invokes onClick on release; the slider needs it live while dragging too.
        public override void OnCursorDown(CursorDownSurfaceEvent e, in Matrix4x4 transform)
        {
            base.OnCursorDown(e, transform);
            if (e.Button is not CursorButton.One) return;
            _dragging = true;
            onClick(ComputeFraction(e.Position));
        }

        // OnCursorMove fires on hover too, not just while dragging - _dragging gates it like SplitterHandleView does.
        public override void OnCursorMove(CursorMoveSurfaceEvent e, in Matrix4x4 transform)
        {
            base.OnCursorMove(e, transform);
            if (_dragging) onClick(ComputeFraction(e.Position));
        }

        public override void OnCursorUp(CursorUpSurfaceEvent e)
        {
            _dragging = false;
            base.OnCursorUp(e);
        }

        private float ComputeFraction(Vector2 cursorPosition)
        {
            var localPosition = cursorPosition.Transform(ComputeAbsoluteContentTransform().Inverse());
            return float.Clamp(localPosition.X / GetSize().X, 0f, 1f);
        }
    }

    private async Task<SurfaceMaterial<PbrMaterial>> LoadGoldMaterial()
    {
        var albedo = _textures.Load(Path.Join(Global.Directory, "assets", "textures", "au_albedo.png"));
        var roughness = _textures.Load(Path.Join(Global.Directory, "assets", "textures", "au_roughness.png"));
        var metallic = _textures.Load(Path.Join(Global.Directory, "assets", "textures", "au_metallic.png"));
        var normal = _textures.Load(Path.Join(Global.Directory, "assets", "textures", "au_normal.png"));

        await Task.WhenAll(albedo, roughness, metallic, normal);
        return new SurfaceMaterial<PbrMaterial>(new PbrMaterial
        {
            BaseColorTexture = albedo.Result,
            RoughnessTexture = roughness.Result,
            MetallicTexture = metallic.Result
        });
    }
}
