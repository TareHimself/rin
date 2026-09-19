using Rin.World.Actors;

namespace Rin.World.Tests;

/// <summary>Exercises <see cref="World.TimeScale" />; per-body composition is covered in <see cref="TimeDilationTests" />.</summary>
public class WorldTimeScaleTests
{
    private sealed class RecordingComponent : Rin.World.Components.WorldComponent
    {
        public readonly List<float> UpdateDeltas = [];

        public override void Update(float deltaSeconds)
        {
            base.Update(deltaSeconds);
            UpdateDeltas.Add(deltaSeconds);
        }
    }

    [Test]
    public void DoubleTimeScaleRunsTwiceAsManyPhysicsStepsForTheSameRealTime()
    {
        var normalPhysics = new FakePhysicsSystem();
        var normalSteps = 0;
        normalPhysics.OnUpdate = _ => normalSteps++;
        var normalWorld = new World(new FakeRenderSystem(), normalPhysics) { TimeScale = 1f };
        normalWorld.Start();

        var fastPhysics = new FakePhysicsSystem();
        var fastSteps = 0;
        fastPhysics.OnUpdate = _ => fastSteps++;
        var fastWorld = new World(new FakeRenderSystem(), fastPhysics) { TimeScale = 2f };
        fastWorld.Start();

        for (var i = 0; i < 60; i++)
        {
            normalWorld.Update(1f / 60f);
            fastWorld.Update(1f / 60f);
        }

        Assert.That(normalSteps, Is.EqualTo(60), "sanity check: 60 real frames at 1x should be ~60 fixed steps");
        Assert.That(fastSteps, Is.EqualTo(2 * normalSteps),
            "2x TimeScale should accumulate scaled time twice as fast, producing twice the fixed steps over the same real time");
    }

    [Test]
    public void ZeroTimeScalePausesPhysicsSteppingAndActorUpdates()
    {
        var physics = new FakePhysicsSystem();
        var steps = 0;
        physics.OnUpdate = _ => steps++;
        var world = new World(new FakeRenderSystem(), physics) { TimeScale = 0f };
        world.Start();

        var recorder = new RecordingComponent();
        world.AddActor(new Actor { RootComponent = recorder });

        for (var i = 0; i < 30; i++) world.Update(1f / 60f);

        Assert.That(steps, Is.EqualTo(0), "TimeScale 0 should never accumulate enough scaled time for a fixed step");
        Assert.That(recorder.UpdateDeltas, Has.All.EqualTo(0f), "actors should see a scaled delta of exactly 0, not the raw real delta");
    }

    [Test]
    public void ActorUpdateReceivesTimeScaledDeltaNotRawDelta()
    {
        var world = new World(new FakeRenderSystem(), new FakePhysicsSystem()) { TimeScale = 3f };
        world.Start();

        var recorder = new RecordingComponent();
        world.AddActor(new Actor { RootComponent = recorder });

        world.Update(1f / 60f);

        Assert.That(recorder.UpdateDeltas, Has.Count.EqualTo(1));
        Assert.That(recorder.UpdateDeltas[0], Is.EqualTo(3f / 60f).Within(1e-6f));
    }
}
