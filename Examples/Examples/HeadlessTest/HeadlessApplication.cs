using Examples.Common;
using System.Numerics;
using Rin.Audio.Null;
using Rin.Core;
using Rin.Core.Audio;
using Rin.Core.Graphics;
using Rin.Core.Views;
using Rin.Graphics.Null;
using Rin.World;
using Rin.World.Physics;
using Rin.World.Physics.Bepu;
using Rin.World.Transforms;

namespace Examples.HeadlessTest;

public sealed class HeadlessApplication : Application
{
    private static readonly Log Log = Log.For<HeadlessApplication>();

    public const string Name = "headless";

    private World? _world;
    private PhysicsFeature? _physics;
    private PhysicsBodyHandle _sphere;
    private float _elapsedSeconds;
    private float _nextPrintAt;

    public override IGraphicsModule CreateGraphicsModule()
    {
        return new NullGraphicsModule();
    }

    public override IAudioModule CreateAudioModule()
    {
        return new NullAudioModule();
    }

    public override IViewsModule CreateViewsModule()
    {
        return new ViewsModule();
    }

    protected override void OnStartup()
    {
        _world = new World { TimeScale = 10f };
        _physics = new PhysicsFeature(_world, new BepuPhysicsSystem());

        var sphere = _world.Spawn();
        _world.SetPosition(sphere, new Vector3(0, 50, 0));
        _world.Add(sphere, new RigidBody { Shape = ColliderShape.Sphere(0.5f), State = PhysicsState.Simulated });
        _sphere = _world.Get<PhysicsBody>(sphere).Handle;
        _physics.PhysicsSystem.SetTimeScale(_sphere, 0.5f); // relative to World.TimeScale => 5x real-time

        OnUpdate += Tick;
    }

    protected override void OnShutdown()
    {
        _physics?.PhysicsSystem.Destroy();
    }

    private void Tick(float deltaSeconds)
    {
        _world!.Update(deltaSeconds);

        _elapsedSeconds += deltaSeconds;
        if (_elapsedSeconds >= _nextPrintAt)
        {
            Log.Info($"t={_elapsedSeconds:F1}s height={_physics!.PhysicsSystem.GetPosition(_sphere).Y:F2}");
            _nextPrintAt += 0.25f;
        }

        if (_elapsedSeconds >= 3f) RequestExit();
    }
}
