using Rin.World.Ecs;
using Rin.World.Ecs.Scheduling;

namespace Rin.World.Tests.Ecs.Scheduling;

public class SystemScheduleTests
{
    // A power of two keeps the accumulator arithmetic exact.
    private const float Interval = 0.125f;

    [Test]
    public void RunsOneTickPerIntervalOfScaledTime()
    {
        var world = new EntityWorld { TickInterval = Interval };
        var ticks = new TickRecorder();
        world.AddSystem(TickPhase.Gameplay, ticks);

        for (var i = 0; i < 8; i++) world.Update(Interval);

        Assert.That(ticks.Ticks, Is.EqualTo(new long[] { 0, 1, 2, 3, 4, 5, 6, 7 }));
        Assert.That(ticks.Deltas, Has.All.EqualTo(Interval));
        Assert.That(world.TickIndex, Is.EqualTo(8));
    }

    [Test]
    public void SixtyFramesAtSixtyHertzRunSixtyTicks()
    {
        var world = new EntityWorld();
        var ticks = new TickRecorder();
        world.AddSystem(TickPhase.Gameplay, ticks);

        for (var i = 0; i < 60; i++) world.Update(1f / 60f);

        Assert.That(ticks.Ticks, Has.Count.EqualTo(60));
    }

    [Test]
    public void TicksPastTheCapAreDropped()
    {
        var world = new EntityWorld { TickInterval = Interval, MaxTicksPerFrame = 2 };
        var ticks = new TickRecorder();
        world.AddSystem(TickPhase.Gameplay, ticks);

        world.Update(Interval * 5);
        Assert.That(ticks.Ticks, Has.Count.EqualTo(2));
        Assert.That(world.InterpolationAlpha, Is.Zero);

        world.Update(0f);
        Assert.That(ticks.Ticks, Has.Count.EqualTo(2), "the dropped backlog must not run on the next frame");
    }

    [Test]
    public void AHitchIsClampedToAQuarterSecond()
    {
        var world = new EntityWorld { TickInterval = Interval, MaxTicksPerFrame = 100 };
        var ticks = new TickRecorder();
        world.AddSystem(TickPhase.Gameplay, ticks);

        world.Update(10f);

        Assert.That(ticks.Ticks, Has.Count.EqualTo(2));
    }

    [Test]
    public void TimeScaleScalesTicksAndFrameDelta()
    {
        var world = new EntityWorld { TickInterval = Interval, TimeScale = 2f };
        var ticks = new TickRecorder();
        var frames = new FrameRecorder();
        world.AddSystem(TickPhase.Gameplay, ticks);
        world.AddSystem(FramePhase.Update, frames);

        world.Update(Interval);

        Assert.That(ticks.Ticks, Has.Count.EqualTo(2));
        Assert.That(frames.Deltas, Is.EqualTo(new[] { Interval * 2 }));
    }

    [Test]
    public void ZeroOrNegativeTimeScalePausesTicks()
    {
        var world = new EntityWorld { TickInterval = Interval, TimeScale = -1f };
        var ticks = new TickRecorder();
        var frames = new FrameRecorder();
        world.AddSystem(TickPhase.Gameplay, ticks);
        world.AddSystem(FramePhase.Update, frames);

        for (var i = 0; i < 10; i++) world.Update(Interval);

        Assert.That(world.TimeScale, Is.Zero);
        Assert.That(ticks.Ticks, Is.Empty);
        Assert.That(frames.Deltas, Has.All.EqualTo(0f));
    }

    [Test]
    public void InterpolationAlphaIsTheLeftoverFractionOfATick()
    {
        var world = new EntityWorld { TickInterval = Interval };
        var frames = new FrameRecorder();
        world.AddSystem(FramePhase.Update, frames);

        world.Update(Interval * 1.5f);

        Assert.That(world.TickIndex, Is.EqualTo(1));
        Assert.That(world.InterpolationAlpha, Is.EqualTo(0.5f));
        Assert.That(frames.Alphas, Is.EqualTo(new[] { 0.5f }));
    }

    [Test]
    public void PhasesRunInFixedOrderAndSystemsInRegistrationOrder()
    {
        var world = new EntityWorld { TickInterval = Interval };
        var log = new List<string>();
        world.AddSystem(FramePhase.Late, new LoggingFrameSystem(log, "late"));
        world.AddSystem(FramePhase.Update, new LoggingFrameSystem(log, "update"));
        world.AddSystem(TickPhase.Physics, new LoggingTickSystem(log, "physics"));
        world.AddSystem(TickPhase.Tasks, new LoggingTickSystem(log, "tasks"));
        world.AddSystem(TickPhase.Gameplay, new LoggingTickSystem(log, "gameplay 1"));
        world.AddSystem(TickPhase.Gameplay, new LoggingTickSystem(log, "gameplay 2"));
        world.AddSystem(TickPhase.Input, new LoggingTickSystem(log, "input"));

        world.Update(Interval);

        Assert.That(log, Is.EqualTo(new[] { "input", "gameplay 1", "gameplay 2", "tasks", "physics", "update", "late" }));
    }

    [Test]
    public void FramePhasesRunEvenWithoutATick()
    {
        var world = new EntityWorld { TickInterval = Interval };
        var ticks = new TickRecorder();
        var frames = new FrameRecorder();
        world.AddSystem(TickPhase.Gameplay, ticks);
        world.AddSystem(FramePhase.Update, frames);

        world.Update(Interval / 2);

        Assert.That(ticks.Ticks, Is.Empty);
        Assert.That(frames.Deltas, Has.Count.EqualTo(1));
    }

    private sealed class TickRecorder : TickSystem
    {
        public readonly List<long> Ticks = [];
        public readonly List<float> Deltas = [];

        public override void Tick(in TickContext context)
        {
            Ticks.Add(context.Tick);
            Deltas.Add(context.DeltaSeconds);
        }
    }

    private sealed class FrameRecorder : FrameSystem
    {
        public readonly List<float> Deltas = [];
        public readonly List<float> Alphas = [];

        public override void Update(in FrameContext context)
        {
            Deltas.Add(context.DeltaSeconds);
            Alphas.Add(context.InterpolationAlpha);
        }
    }

    private sealed class LoggingTickSystem(List<string> log, string name) : TickSystem
    {
        public override void Tick(in TickContext context) => log.Add(name);
    }

    private sealed class LoggingFrameSystem(List<string> log, string name) : FrameSystem
    {
        public override void Update(in FrameContext context) => log.Add(name);
    }
}
