using System.Numerics;
using Rin.Audio.Null;
using Rin.Core;
using Rin.Core.Audio;
using Rin.Core.Graphics;
using Rin.Core.Shared.Math;
using Rin.Core.Views;
using Rin.Graphics.Null;
using Rin.World;
using Rin.World.Graphics.Default;
using Rin.World.Physics;
using Rin.World.Physics.Bepu;

namespace HeadlessTest;

public class MainApplication : Application
{
    private World? _world;
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
        _world = new World(new DefaultRenderSystem(), new BepuPhysicsSystem()) { TimeScale = 10f };
        _sphere = _world.PhysicsSystem.CreateSphere(0.5f,
            new Transform { Position = new Vector3(0, 50, 0) }, PhysicsState.Simulated);
        _world.PhysicsSystem.SetTimeScale(_sphere, 0.5f); // relative to World.TimeScale => 5x real-time
        _world.Start();

        OnUpdate += Tick;
    }

    protected override void OnShutdown()
    {
        _world?.Stop();
    }

    private void Tick(float deltaSeconds)
    {
        _world!.Update(deltaSeconds);

        _elapsedSeconds += deltaSeconds;
        if (_elapsedSeconds >= _nextPrintAt)
        {
            Console.WriteLine($"t={_elapsedSeconds:F1}s height={_world.PhysicsSystem.GetPosition(_sphere).Y:F2}");
            _nextPrintAt += 0.25f;
        }

        if (_elapsedSeconds >= 3f) RequestExit();
    }
}
