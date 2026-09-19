using System.Numerics;
using Rin.World;
using Rin.World.Actors;
using Rin.World.Components;
using Rin.World.Components.Lights;
using Rin.World.Graphics.Default;
using Rin.World.Mesh;
using Rin.World.Physics;
using Rin.World.Physics.Bepu;
using Examples.Common;
using Examples.Common.Views;
using SceneTest.entities;
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
using experiments.Docking.Model;
using experiments.Docking.Views;

namespace SceneTest;

public class SceneTestApplication : ExampleApplication
{
    private readonly Random _rng = new();
    private StaticMesh? _cubeMesh;
    private DefaultMeshMaterial? _material;
    private World? _scene;

    protected override void OnStartup()
    {
        IViewsModule.Get().OnSurfaceCreated += surf =>
        {
            var scene = _scene = new World(new DefaultRenderSystem(), new BepuPhysicsSystem());
            scene.Start();

            // A perspective camera looking down +Z at the play area, and a top-down camera.
            var perspectiveCam = scene.AddActor<CameraActor>();
            perspectiveCam.GetCameraComponent().SetLocation(new Vector3(0f, 10f, -40f));

            var topCam = scene.AddActor<CameraActor>();
            topCam.GetCameraComponent().SetLocation(new Vector3(0f, 80f, 20f));
            topCam.GetCameraComponent().SetRotation(MathR.LookTowards(new Vector3(0f, -1f, 0f)));

            // Static ground.
            scene.AddActor(new Actor
            {
                RootComponent = new BoxPhysicsComponent
                {
                    Size = new Vector3(200f, 1f, 200f),
                    Location = new Vector3(0f, -6f, 20f),
                    Scale = new Vector3(200f, 1f, 200f),
                    PhysicsState = PhysicsState.Static
                }
            });

            Extensions.LoadStaticMesh(Path.Join(Global.Directory, "assets", "models", "cube.glb")).After(mesh =>
            {
                _cubeMesh = mesh;

                scene.AddPointLight(new Vector3(0f, 35f, 20f));
                scene.AddPointLight(new Vector3(0f, -20f, 0f));

                var directionalLight = scene.AddActor(new Actor
                {
                    RootComponent = new DirectionalLightComponent
                    {
                        Radiance = 10.0f,
                        Location = new Vector3(0.0f, 200.0f, 0.0f)
                    }
                });
                directionalLight.SetRotation(
                    MathR.LookTowards(Vector3.Normalize(Vector3.Zero - directionalLight.GetLocation())));

                // Static visible obstacle the dynamic boxes pile against.
                scene.AddActor(new Actor
                {
                    RootComponent = new BoxPhysicsComponent
                    {
                        Size = new Vector3(10f, 5f, 10f),
                        Location = new Vector3(0f, -2.5f, 22f),
                        Scale = new Vector3(10f, 5f, 10f),
                        PhysicsState = PhysicsState.Static
                    },
                    InitialComponents = [new StaticMeshComponent { Mesh = mesh }]
                });

                OnUpdate += scene.Update;

                LoadGoldMaterial().After(material =>
                {
                    _material = material;
                    DropBoxes(30);
                });

                Extensions.LoadSkinnedMesh(Path.Join(Global.Directory, "assets", "models", "fox.glb"))
                    .After(skinned =>
                    {
                        if (skinned is null) return;
                        LoadGoldMaterial().After(material =>
                            IApplication.Get().MainDispatcher.Enqueue(() =>
                            {
                                foreach (var x in new[] { -30f, 0f, 30f })
                                    scene.AddActor(new Actor
                                    {
                                        RootComponent = new SkinnedMeshComponent
                                        {
                                            Mesh = skinned,
                                            Materials = [material],
                                            PoseSource = new TestPoseSource { Skeleton = skinned.Skeleton },
                                            Location = new Vector3(x, -5f, 55f)
                                        }
                                    });
                            }));
                    });
            });

            var window = surf.Renderer.GetWindow();
            window.OnClose += _ =>
            {
                if (window.Parent != null)
                    window.Dispose();
                else
                    RequestExit();
            };
            window.OnKey += e =>
            {
                if (e is { Key: InputKey.P, State: InputState.Pressed }) DropBoxes(15);
            };

            surf.Add(BuildDockLayout(perspectiveCam, topCam, scene));
        };

        IGraphicsModule.Get()
            .CreateWindow("Rin Scene Test", new Extent2D(1280, 800), WindowFlags.Visible | WindowFlags.Resizable);
    }

    protected override void OnShutdown()
    {
    }

    /// <summary>Spawn <paramref name="count" /> dynamic cubes above the play area to fall and collide.</summary>
    private void DropBoxes(int count)
    {
        // Actors must be added on the main thread — AddActor runs Start() (creates the physics body)
        // and scene.Update walks the actors concurrently. This may be called from a task continuation.
        IApplication.Get().MainDispatcher.Enqueue(() => SpawnBoxes(count));
    }

    private void SpawnBoxes(int count)
    {
        if (_scene is not { } scene || _cubeMesh is not { } mesh) return;

        for (var i = 0; i < count; i++)
        {
            var pos = new Vector3(
                (float)(_rng.NextDouble() * 10.0 - 5.0),
                26f + i * 3f,
                22f + (float)(_rng.NextDouble() * 10.0 - 5.0));

            scene.AddActor(new Actor
            {
                RootComponent = new BoxPhysicsComponent
                {
                    Size = new Vector3(2f),
                    Location = pos,
                    Scale = new Vector3(2f),
                    PhysicsState = PhysicsState.Simulated
                },
                InitialComponents =
                [
                    new StaticMeshComponent
                    {
                        Mesh = mesh,
                        Materials = _material is { } m ? [m] : []
                    }
                ]
            });
        }
    }

    private static DockSpaceView BuildDockLayout(CameraActor perspectiveCam, CameraActor topCam, World scene)
    {
        DockPanel Panel(string id, string title, IView content, Color? bg = null)
        {
            return new DockPanel(id, title, new RectView
            {
                Color = bg ?? new Color(0.12f, 0.12f, 0.14f, 1f),
                Padding = new Padding(10f),
                InitChild = content
            });
        }

        var viewports = new DockSplitNode(DockOrientation.Vertical,
            new DockTabGroupNode(new DockPanel("perspective", "Perspective", new TestViewport(perspectiveCam))),
            new DockTabGroupNode(new DockPanel("top", "Top", new TestViewport(topCam))));

        var stats = new DockTabGroupNode(Panel("stats", "Stats", new FpsView(),
            new Color(0.10f, 0.10f, 0.12f, 1f)));
        var controls = new DockTabGroupNode(Panel("controls", "Controls",
            new FlexBoxView(Axis.Column)
            {
                InitSlots =
                [
                    new FlexBoxSlot
                    {
                        Child = new TextBoxView
                        {
                            Content = "RMB drag  — look\nWASD      — move\nLMB       — cycle view channel\nP         — drop boxes",
                            FontSize = 14f,
                            WrapContent = true
                        }
                    },
                    new FlexBoxSlot { Child = BuildTimeScaleControl(scene), Flex = 1, Fit = CrossFit.Fill }
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
            BackgroundColor = new Color(0.2f, 0.2f, 0.22f, 1f),
            ForegroundColor = new Color(0.3f, 0.6f, 1f, 1f),
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

        protected override Vector2 LayoutContent(in Vector2 availableSpace)
        {
            return base.LayoutContent(availableSpace with { Y = Height });
        }

        // Base ProgressBarView only invokes onClick on release; the slider needs it live while dragging too.
        public override void OnCursorDown(CursorDownSurfaceEvent e, in Matrix4x4 transform)
        {
            base.OnCursorDown(e, transform);
            if (e.Button is CursorButton.One) onClick(ComputeFraction(e.Position));
        }

        public override void OnCursorMove(CursorMoveSurfaceEvent e, in Matrix4x4 transform)
        {
            base.OnCursorMove(e, transform);
            onClick(ComputeFraction(e.Position));
        }

        private float ComputeFraction(Vector2 cursorPosition)
        {
            var localPosition = cursorPosition.Transform(ComputeAbsoluteContentTransform().Inverse());
            return localPosition.X / GetSize().X;
        }
    }

    public static async Task<ResourceHandle> LoadTexture(string path)
    {
        using var imgData = await Task.Run(() => HostImage.Create(File.OpenRead(path)));
        var task = imgData.CreateTexture(out var handle);
        await task;
        return handle;
    }

    public static async Task<DefaultMeshMaterial> LoadGoldMaterial()
    {
        var albedo = LoadTexture(Path.Join(Global.Directory, "assets", "textures", "au_albedo.png"));
        var roughness = LoadTexture(Path.Join(Global.Directory, "assets", "textures", "au_roughness.png"));
        var metallic = LoadTexture(Path.Join(Global.Directory, "assets", "textures", "au_metallic.png"));
        var normal = LoadTexture(Path.Join(Global.Directory, "assets", "textures", "au_normal.png"));

        await Task.WhenAll(albedo, roughness, metallic, normal);
        return new DefaultMeshMaterial
        {
            ColorImageId = albedo.Result,
            RoughnessImageId = roughness.Result,
            MetallicImageId = metallic.Result
        };
    }
}
