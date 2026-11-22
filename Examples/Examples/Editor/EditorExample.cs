using System.Numerics;
using Examples.Common;
using Examples.Common.Views;
using Examples.Editor.Model;
using Examples.Editor.Views;
using Examples.SceneTest;
using experiments.Docking.Model;
using experiments.Docking.Views;
using Rin.Core;
using Rin.Core.Extensions;
using Rin.Core.Graphics;
using Rin.Core.Shared.Math;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Layouts;
using Rin.GLTF;
using Rin.World;
using Rin.World.Graphics;
using Rin.World.Graphics.Default;
using Rin.World.Graphics.ImageBasedLighting;
using Rin.World.Graphics.Mesh;
using Rin.World.Physics;
using Rin.World.Physics.Bepu;
using Rin.World.Transforms;
using Camera = Rin.World.Graphics.Camera;
using StaticMesh = Rin.World.Graphics.StaticMesh;

namespace Examples.Editor;

/// <summary>
///     A test editor over a world: a hierarchy of every entity, an inspector built from the component schema, a viewport,
///     and a <see cref="PlaySession" /> that plays, pauses and steps a copy of the edited world.
///     Run it as <c>editor</c> for a small demo scene, or <c>editor path/to/level-folder</c> to edit a glTF level.
/// </summary>
public sealed class EditorExample : Example
{
    private const string SkyName = "footprint_court";
    private const float SkyIntensity = 1.2f;
    private const float CameraPitch = 15f;
    private const float CameraSpeed = 10f;

    private static readonly Log Log = Log.For<EditorExample>();

    private readonly EntityNames _names = new();
    private PlaySession? _session;
    private RenderFeature? _render;
    private BepuPhysicsSystem? _physicsSystem;
    private IMeshFactory? _meshFactory;

    public override string Name => "editor";

    public override string Title => "Editor";

    public override Extent2D WindowSize => new(1600, 900);

    public override void Start(ExampleContext context)
    {
        IconFont.Load();
        var meshFactory = _meshFactory = Global.Provider.AddSingle<IMeshFactory>(new MeshFactory());
        var world = new World(ComponentSchema.Compose(ComponentModule.Engine, GeneratedComponents.Module))
        {
            TimeScale = 0f
        };
        var render = _render = new RenderFeature(world, new DefaultRenderSystem(), meshFactory);
        var physics = new PhysicsFeature(world, _physicsSystem = new BepuPhysicsSystem());

        var levelPath = context.Args.FirstOrDefault(IsLevelPath);
        var camera = levelPath is not null ? LoadLevel(levelPath, render, physics) : SpawnDemoScene(render);
        var sceneName = levelPath is not null ? Path.GetFileName(Path.TrimEndingDirectorySeparator(levelPath)) : "demo scene";
        world.Update(0f);

        var selection = new EditorSelection();
        var session = _session = new PlaySession(world);
        context.OnUpdate(session.Update);
        context.Add(Ui.Column(
            Ui.Slot(new EditorToolbar(session, sceneName)),
            Ui.Slot(BuildLayout(render, camera, selection), 1f),
            Ui.Slot(new StatusBarView(session, world, selection, _names))));
    }

    public override void Stop()
    {
        _render?.Environments.Dispose();
        _render?.RenderSystem.Dispose();
        _render = null;
        _physicsSystem?.Destroy();
        _physicsSystem = null;
        _meshFactory?.Dispose();
        _meshFactory = null;
        _session?.Clear();
        _session = null;
        _names.Clear();
    }

    // The example's own name is an argument too, and matches its source folder when run from Examples/Examples.
    private bool IsLevelPath(string argument) =>
        (argument != Name && Directory.Exists(argument)) ||
        argument.EndsWith(".glb", StringComparison.OrdinalIgnoreCase) ||
        argument.EndsWith(".gltf", StringComparison.OrdinalIgnoreCase);

    private Entity LoadLevel(string path, RenderFeature render, PhysicsFeature physics)
    {
        var level = GltfSceneImporter.Load(path, render, _ => null, physics.Shapes);
        foreach (var warning in level.Warnings) Log.Warn(warning);
        _names.SetAll(level.Names);
        return level.StartCamera ?? SpawnCamera(render.World, new Vector3(0f, 1.7f, -5f));
    }

    private Entity SpawnDemoScene(RenderFeature render)
    {
        var world = render.World;
        var camera = SpawnCamera(world, new Vector3(0f, 6f, -18f));
        _names.Set(camera, "Camera");

        var sun = world.Spawn();
        _names.Set(sun, "Sun");
        world.SetRotation(sun, MathR.LookTowards(Vector3.Normalize(new Vector3(0.3f, -1f, 0.4f))));
        world.Add(sun, new DirectionalLight { Lux = 12f });

        Task.Run(() => LoadSky()).DispatchMain(sky =>
        {
            if (_render != render || sky is null) return;
            var skyEntity = world.Spawn();
            world.Add(skyEntity, new EnvironmentLight { Map = render.Environments.Create(sky), Intensity = SkyIntensity });
            _names.Set(skyEntity, "Sky");
        });

        var cubePath = Path.Join(Global.Directory, "assets", "models", "cube.glb");
        Task.Run(() => GltfMeshImporter.LoadStaticMesh(cubePath)).DispatchMain(loaded =>
        {
            if (_render != render || loaded is not { } mesh) return;

            var materials = render.Materials.Add(DefaultMaterials.Pbr);
            var ground = SpawnBox(world, new Vector3(0f, -1f, 0f), new Vector3(20f, 1f, 20f), PhysicsState.Static, mesh,
                materials);
            _names.Set(ground, "Ground");
            for (var i = 0; i < 4; i++)
            {
                var box = SpawnBox(world, new Vector3(i * 1.5f - 2f, 4f + i * 3f, i * 0.5f), new Vector3(1f),
                    PhysicsState.Simulated, mesh, materials);
                _names.Set(box, $"Box {i + 1}");
            }

            var post = SpawnBox(world, new Vector3(-8f, 2f, 6f), new Vector3(0.4f, 3f, 0.4f), PhysicsState.Static, mesh,
                materials);
            var lamp = world.Spawn();
            world.SetPosition(lamp, new Vector3(0f, 1.2f, 0f));
            world.SetParent(lamp, post);
            world.Add(lamp, new PointLight { Lumens = 8000f });
            _names.Set(post, "Post");
            _names.Set(lamp, "Point light");
        });

        return camera;
    }

    private static Entity SpawnCamera(World world, Vector3 position)
    {
        var camera = world.Spawn();
        world.SetPosition(camera, position);
        world.SetRotation(camera, MathR.Forward.ToQuaternion().AddLocalPitch(CameraPitch));
        world.Add(camera, new Camera { Ev100 = 0f });
        return camera;
    }

    // The physics system multiplies the box size by the entity scale, as in the scene test.
    private static Entity SpawnBox(World world, Vector3 position, Vector3 size, PhysicsState state, MeshHandle mesh,
        MaterialSet materials)
    {
        var box = world.Spawn();
        world.SetLocalTransform(box, new LocalTransform { Position = position, Scale = size });
        world.Add(box, new RigidBody { State = state });
        world.Add(box, new BoxCollider { Size = size });
        world.Add(box, new StaticMesh { Mesh = mesh, Materials = materials });
        return box;
    }

    // Downloaded on first run, so the editor still starts without a sky when offline.
    private static async Task<EnvironmentMap?> LoadSky()
    {
        try
        {
            return EnvironmentMap.FromRadiance(await SampleEnvironments.Load(SkyName));
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or IOException
                                      or InvalidDataException)
        {
            Log.Warn($"Could not load the sky '{SkyName}' ({e.Message})");
            return null;
        }
    }

    private DockSpaceView BuildLayout(RenderFeature render, Entity camera, EditorSelection selection)
    {
        var world = render.World;
        var hierarchy = Panel("hierarchy", "Hierarchy", HierarchyPanel.Create(world, selection, _names));
        var viewport = new DockPanel("viewport", "Viewport",
            new TestViewport(render, camera, CameraPitch, speed: CameraSpeed));
        var inspector = Panel("inspector", "Inspector", new InspectorView(world, selection, _names));

        var root = new DockSplitNode(DockOrientation.Horizontal,
            new DockTabGroupNode(hierarchy), new DockTabGroupNode(viewport), new DockTabGroupNode(inspector));
        root.Weights[0] = 0.2f;
        root.Weights[1] = 0.52f;
        root.Weights[2] = 0.28f;
        root.NormalizeWeights();
        return new DockSpaceView(new DockTree(root));
    }

    private static DockPanel Panel(string id, string title, IView content) =>
        new(id, title, new RectView { Color = EditorTheme.Panel, Padding = new Padding(12f), InitChild = content });
}
