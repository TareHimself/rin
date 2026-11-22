using System.Globalization;
using System.Numerics;
using Examples.Common;
using Examples.SceneTest;
using Rin.Core;
using Rin.Core.Shared.Math;
using Rin.GLTF;
using Rin.World;
using Rin.World.Graphics;
using Rin.World.Graphics.Default;
using Rin.World.Graphics.Mesh;
using Rin.World.Physics;
using Rin.World.Physics.Bepu;
using Rin.World.Transforms;

namespace Examples.Level;

/// <summary>
///     Opens a glTF level, such as one exported by the Rin Blender add-on, from its start camera, with its rigid bodies
///     simulated.
///     Run it as <c>level path/to/level-folder [--ev100=N]</c>, or with a .glb in place of the folder.
/// </summary>
public sealed class LevelExample : Example
{
    private const string Ev100Argument = "--ev100=";
    private const float MoveSpeed = 5f;

    // Matches a Blender view at Exposure +9, the setting for a level lit by real bulbs.
    private const float DefaultEv100 = 0f;

    private static readonly Log Log = Log.For<LevelExample>();

    private RenderFeature? _render;
    private IMeshFactory? _meshFactory;
    private BepuPhysicsSystem? _physicsSystem;

    public override string Name => "level";

    public override string Title => "Level";

    public override void Start(ExampleContext context)
    {
        if (context.Args.FirstOrDefault(IsLevelPath) is not { } path)
        {
            Log.Warn("Pass a level to open: level path/to/level-folder [--ev100=N]");
            return;
        }

        var meshFactory = _meshFactory = Global.Provider.AddSingle<IMeshFactory>(new MeshFactory());
        // Composed with this game's module, so the level's components are found by name.
        var world = new World(ComponentSchema.Compose(ComponentModule.Engine, GeneratedComponents.Module));
        var render = _render = new RenderFeature(world, new DefaultRenderSystem(), meshFactory);
        var physics = new PhysicsFeature(render.World, _physicsSystem = new BepuPhysicsSystem());

        var level = GltfSceneImporter.Load(path, render, _ => null, physics.Shapes);
        foreach (var warning in level.Warnings) Log.Warn(warning);

        var camera = level.StartCamera ?? SpawnCamera(render.World);
        render.World.Write<Camera>(camera).Ev100 = ReadEv100(context.Args);
        render.World.Update(0f);

        var (pitch, yaw) = PitchAndYawOf(render.World.Get<LocalTransform>(camera).Rotation);
        context.Add(new TestViewport(render, camera, pitch, yaw, MoveSpeed));
        context.OnUpdate(render.World.Update);
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
    }

    private static bool IsLevelPath(string argument) =>
        Directory.Exists(argument) ||
        argument.EndsWith(".glb", StringComparison.OrdinalIgnoreCase) ||
        argument.EndsWith(".gltf", StringComparison.OrdinalIgnoreCase);

    private static float ReadEv100(string[] args)
    {
        var argument = args.FirstOrDefault(a => a.StartsWith(Ev100Argument, StringComparison.Ordinal));
        return argument is not null &&
               float.TryParse(argument[Ev100Argument.Length..], CultureInfo.InvariantCulture, out var ev100)
            ? ev100
            : DefaultEv100;
    }

    private static Entity SpawnCamera(World world)
    {
        var camera = world.Spawn();
        world.SetPosition(camera, new Vector3(0f, 1.7f, -5f));
        world.Add(camera, new Camera());
        return camera;
    }

    // The inverse of MathR.LookTowards, so TestViewport's mouse look continues from the imported rotation.
    private static (float Pitch, float Yaw) PitchAndYawOf(Quaternion rotation)
    {
        var forward = rotation.GetForward();
        var pitch = float.RadiansToDegrees(-MathF.Asin(float.Clamp(forward.Y, -1f, 1f)));
        var yaw = float.RadiansToDegrees(MathF.Atan2(forward.X, forward.Z));
        return (pitch, yaw);
    }
}
