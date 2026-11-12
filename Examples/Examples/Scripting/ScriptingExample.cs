using System.Numerics;
using Examples.Common;
using Examples.SceneTest;
using Rin.Core;
using Rin.Core.Extensions;
using Rin.Core.Graphics.Windows;
using Rin.Core.Graphics.Windows.Events;
using Rin.Core.Shared.Math;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Content;
using Rin.Core.Views.Layouts;
using Rin.GLTF;
using Rin.World;
using Rin.World.Graphics;
using Rin.World.Graphics.Default;
using Rin.World.Graphics.Mesh;
using Rin.World.Graphics.Surfaces;
using Rin.World.Physics;
using Rin.World.Physics.Bepu;
using Rin.World.Scripting;
using Rin.World.Transforms;
using Camera = Rin.World.Graphics.Camera;

namespace Examples.Scripting;

/// <summary>
///     A small arena where every moving thing is driven by one <see cref="Script" />, each showing one way to use
///     <c>Tick</c>, <c>GameTask</c> and <c>TaskContext</c>.
/// </summary>
public sealed class ScriptingExample : Example
{
    private const float CameraPitch = 35f;
    private const float RespawnSeconds = 2f;

    private const string HelpText =
        """
        Each object runs one script. The console logs every task event with its tick.

        Gold cube, Patroller: moves in Tick, every fixed tick
        Green gate, SlidingDoor: Until the patroller is near, then Seconds
        Red cube, Bomb: WhenAny(fuse, Defuse event)
        Blue pillar, ChargingLight: catch and finally on cancellation
        Brown crates, CrateDropper: a Seconds loop with World.Random
        Respawner brings back the bomb and the pillar, Lifetime clears debris

        F: raise Defuse on the bomb before its fuse runs out
        K: despawn the pillar mid-charge
        Click the view, then RMB drag to look and W, A, S, D to move
        """;

    private RenderFeature? _render;
    private PhysicsFeature? _physics;
    private IMeshFactory? _meshFactory;
    private Entity _bomb;
    private Entity _charger;

    public override string Name => "scripting";

    public override string Title => "Scripting";

    public override void Start(ExampleContext context)
    {
        var meshFactory = _meshFactory = Global.Provider.AddSingle<IMeshFactory>(new MeshFactory());

        var world = new World();
        var render = _render = new RenderFeature(world, new DefaultRenderSystem(), meshFactory);
        _physics = new PhysicsFeature(world, new BepuPhysicsSystem());
        var scripts = new ScriptFeature(world);

        var camera = world.Spawn();
        world.SetPosition(camera, new Vector3(0f, 20f, -28f));
        world.SetRotation(camera, MathR.Forward.ToQuaternion().AddLocalPitch(CameraPitch));
        world.Add(camera, new Camera());
        SpawnLights(world);

        LoadAssets(context.Application.Textures).DispatchMain(assets =>
        {
            if (_render != render) return;
            BuildArena(new Arena(render, scripts, assets.CubeMesh, assets.Gold));
        });

        context.OnKey(e => OnKey(e, world, scripts));
        context.OnUpdate(world.Update);
        context.Add(new TestViewport(render, camera, CameraPitch));
        context.Add(BuildHelpPanel());
    }

    public override void Stop()
    {
        _physics?.PhysicsSystem.Destroy();
        _physics = null;
        _render?.RenderSystem.Dispose();
        _render = null;
        _meshFactory?.Dispose();
        _meshFactory = null;
        _bomb = default;
        _charger = default;
    }

    private void BuildArena(Arena arena)
    {
        var world = arena.World;
        arena.SpawnBody(new Vector3(0f, -0.5f, 0f), new Vector3(32f, 1f, 32f), arena.Stone, PhysicsState.Static);

        var patroller = arena.SpawnBody(new Vector3(0f, 1f, -2f), new Vector3(2f), arena.Gold, PhysicsState.Controlled);
        arena.Scripts.Attach(patroller, new Patroller(new Vector3(0f, 1f, -2f), 9f, 8f));

        var doorClosed = new Vector3(12f, 2f, -2f);
        var door = arena.SpawnBody(doorClosed, new Vector3(1f, 4f, 4f), arena.Gate, PhysicsState.Controlled);
        world.Add(door, new DoorState());
        arena.Scripts.Attach(door, new SlidingDoor(patroller, doorClosed, doorClosed + new Vector3(0f, 0f, 4.5f)));

        arena.Scripts.Attach(world.Spawn(), new Respawner("bomb", RespawnSeconds, () =>
        {
            _bomb = arena.SpawnCube(new Vector3(-7f, 0.75f, 7f), new Vector3(1.5f), arena.Bomb);
            arena.Scripts.Attach(_bomb, new Bomb(arena));
            return _bomb;
        }));

        arena.Scripts.Attach(world.Spawn(), new Respawner("pillar", RespawnSeconds, () =>
        {
            _charger = arena.SpawnCube(new Vector3(7f, 0.2f, 7f), new Vector3(1.2f, 0.4f, 1.2f), arena.Charger);
            arena.Scripts.Attach(_charger, new ChargingLight(arena));
            return _charger;
        }));

        arena.Scripts.Attach(world.Spawn(), new CrateDropper(arena, 3f, 12f));
    }

    // Key events are handed to the main dispatcher so they never land in the middle of world.Update.
    private void OnKey(KeyEvent e, World world, ScriptFeature scripts)
    {
        if (e.State != InputState.Pressed) return;

        if (e.Key == InputKey.F)
            IApplication.Get().MainDispatcher.Enqueue(() => scripts.Tasks.Raise(_bomb, new Defuse()));

        if (e.Key == InputKey.K)
            IApplication.Get().MainDispatcher.Enqueue(() =>
            {
                if (world.IsAlive(_charger)) world.Despawn(_charger);
            });
    }

    private static void SpawnLights(World world)
    {
        var sun = world.Spawn();
        var sunPosition = new Vector3(10f, 50f, -20f);
        world.SetPosition(sun, sunPosition);
        world.SetRotation(sun, MathR.LookTowards(-sunPosition));
        world.Add(sun, new DirectionalLight { Radiance = 4f });

        var fill = world.Spawn();
        world.SetPosition(fill, new Vector3(0f, 25f, -10f));
        world.Add(fill, new PointLight { Radiance = 400f });
    }

    private static IView BuildHelpPanel()
    {
        return new PanelView
        {
            InitSlots =
            [
                new PanelSlot
                {
                    Child = new RectView
                    {
                        Color = new Color(0.08f, 0.08f, 0.1f, 0.8f),
                        Padding = new Padding(12f),
                        InitChild = new TextBoxView { Content = HelpText, FontSize = 14f }
                    },
                    Offset = new Vector2(12f),
                    SizeToContent = true
                }
            ]
        };
    }

    private static async Task<(MeshHandle CubeMesh, SurfaceMaterial<PbrMaterial> Gold)> LoadAssets(TextureCache textures)
    {
        var cubePath = Path.Join(Global.Directory, "assets", "models", "cube.glb");
        var cube = Task.Run(() => GltfMeshImporter.LoadStaticMesh(cubePath));
        var gold = LoadGoldMaterial(textures);
        await Task.WhenAll(cube, gold);
        var mesh = cube.Result ?? throw new InvalidOperationException("cube.glb did not load.");
        return (mesh, gold.Result);
    }

    private static async Task<SurfaceMaterial<PbrMaterial>> LoadGoldMaterial(TextureCache textures)
    {
        var albedo = textures.Load(Path.Join(Global.Directory, "assets", "textures", "au_albedo.png"));
        var roughness = textures.Load(Path.Join(Global.Directory, "assets", "textures", "au_roughness.png"));
        var metallic = textures.Load(Path.Join(Global.Directory, "assets", "textures", "au_metallic.png"));

        await Task.WhenAll(albedo, roughness, metallic);
        return new SurfaceMaterial<PbrMaterial>(new PbrMaterial
        {
            BaseColorTexture = albedo.Result,
            RoughnessTexture = roughness.Result,
            MetallicTexture = metallic.Result
        });
    }
}
