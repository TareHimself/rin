using Rin.World.Ecs;
using Rin.World.Ecs.Scheduling;
using Rin.World.Ecs.Tasks;

namespace Rin.World.Tests.Ecs.Tasks;

public class TaskContextTests
{
    private const float Interval = 0.125f;

    private EntityWorld _world = null!;
    private GameTaskScheduler _tasks = null!;
    private List<string> _log = null!;

    [SetUp]
    public void SetUp()
    {
        _world = new EntityWorld { TickInterval = Interval };
        _tasks = new GameTaskScheduler(_world);
        _log = [];
    }

    [Test]
    public void TicksResumesInTheNthTasksPhase()
    {
        _tasks.Run(default, async ctx =>
        {
            await ctx.Ticks(3);
            Log("three");
            await ctx.NextTick();
            Log("next");
        });

        Tick(5);

        Assert.That(_log, Is.EqualTo(new[] { "three@2", "next@3" }), "awaited before the first tick, Ticks(1) is tick 0");
    }

    [Test]
    public void NextTickAwaitedInGameplayResumesInTheSameTick()
    {
        _world.AddSystem(TickPhase.Gameplay, new RunOnce(() => _tasks.Run(default, async ctx =>
        {
            await ctx.NextTick();
            Log("resumed");
        })));

        Tick(2);

        Assert.That(_log, Is.EqualTo(new[] { "resumed@0" }));
    }

    [Test]
    public void ZeroTicksAndZeroSecondsDoNotSuspend()
    {
        _tasks.Run(default, async ctx =>
        {
            await ctx.Ticks(0);
            await ctx.Seconds(0f);
            Log("done");
        });

        Assert.That(_log, Is.EqualTo(new[] { "done@0" }));
        Assert.That(_tasks.RunningCount, Is.Zero);
    }

    [TestCase(0.125f, 0.5f, 4)]
    [TestCase(0.125f, 0.3f, 3)]
    [TestCase(0.125f, 0.01f, 1)]
    [TestCase(0.1f, 0.3f, 3)]
    [TestCase(1f / 60f, 0.5f, 30)]
    public void SecondsRoundsUpToWholeTicks(float tickInterval, float seconds, int expectedTicks)
    {
        _world.TickInterval = tickInterval;
        var resumedAt = -1L;
        _tasks.Run(default, async ctx =>
        {
            await ctx.Seconds(seconds);
            resumedAt = _world.TickIndex;
        });

        for (var i = 0; i < expectedTicks + 2; i++) _world.Update(tickInterval);

        Assert.That(resumedAt, Is.EqualTo(expectedTicks - 1));
    }

    [Test]
    public void UntilIsCheckedOncePerTasksPhase()
    {
        var checks = 0;
        _tasks.Run(default, async ctx =>
        {
            await ctx.Until(() => ++checks == 3);
            Log("open");
        });

        Tick(5);

        Assert.That(_log, Is.EqualTo(new[] { "open@2" }));
        Assert.That(checks, Is.EqualTo(3));
    }

    [Test]
    public void AnExceptionInAnUntilConditionFaultsTheWait()
    {
        var caught = new List<string>();
        _tasks.Run(default, async ctx =>
        {
            try
            {
                await ctx.Until(() => throw new InvalidOperationException("broken"));
            }
            catch (InvalidOperationException e)
            {
                caught.Add(e.Message);
            }
        });

        Tick(1);

        Assert.That(caught, Is.EqualTo(new[] { "broken" }));
    }

    [Test]
    public void EventDeliversTheFirstValueRaisedForItsEntityAfterTheWait()
    {
        var door = _world.Spawn();
        var other = _world.Spawn();
        _tasks.Raise(door, new Knock(1));
        var received = new List<int>();
        _tasks.Run(default, async ctx =>
        {
            received.Add((await ctx.Event<Knock>(door)).Strength);
            received.Add((await ctx.Event<Knock>(door)).Strength);
        });

        _tasks.Raise(other, new Knock(2));
        _tasks.Raise(door, new Knock(3));
        _tasks.Raise(door, new Knock(4));
        Tick(1);
        Assert.That(received, Is.EqualTo(new[] { 3 }), "the raise before the wait, the other entity's and the second raise are not seen");

        _tasks.Raise(door, new Knock(5));
        Tick(1);
        Assert.That(received, Is.EqualTo(new[] { 3, 5 }));
    }

    [Test]
    public void AnEventRaisedInGameplayIsDeliveredInTheSameTick()
    {
        var door = _world.Spawn();
        _tasks.Run(default, async ctx =>
        {
            await ctx.Event<Knock>(door);
            Log("knocked");
        });
        _world.AddSystem(TickPhase.Gameplay, new RunOnce(() => _tasks.Raise(door, new Knock(1))));

        Tick(1);

        Assert.That(_log, Is.EqualTo(new[] { "knocked@0" }));
    }

    [Test]
    public void WhenAnyReturnsTheWinnerAndCancelsTheLoser()
    {
        var door = _world.Spawn();
        var winners = new List<int>();
        _tasks.Run(default, async ctx =>
        {
            winners.Add(await ctx.WhenAny(LoggedWait(ctx, 5, "slow"), ctx.Event<Knock>(door)));
            winners.Add(await ctx.WhenAny(ctx.Ticks(1), ctx.Ticks(2)));
        });

        _tasks.Raise(door, new Knock(1));
        Tick(8);

        Assert.That(winners, Is.EqualTo(new[] { 1, 0 }));
        Assert.That(_log, Is.Empty, "the cancelled loser never resumes");
        Assert.That(_tasks.RunningCount, Is.Zero);
    }

    [Test]
    public void WhenAllWaitsForBoth()
    {
        _tasks.Run(default, async ctx =>
        {
            await ctx.WhenAll(LoggedWait(ctx, 2, "a"), LoggedWait(ctx, 4, "b"));
            Log("all");
        });

        Tick(6);

        Assert.That(_log, Is.EqualTo(new[] { "a@1", "b@3", "all@3" }));
    }

    [Test]
    public void WhenAllFaultsAndCancelsTheOtherWhenOneFaults()
    {
        var caught = new List<string>();
        _tasks.Run(default, async ctx =>
        {
            try
            {
                await ctx.WhenAll(Throwing(ctx), LoggedWait(ctx, 4, "slow"));
            }
            catch (InvalidOperationException e)
            {
                caught.Add(e.Message);
            }
        });

        Tick(6);

        Assert.That(caught, Is.EqualTo(new[] { "thrown" }));
        Assert.That(_log, Is.Empty);
    }

    private async GameTask LoggedWait(TaskContext ctx, int ticks, string name)
    {
        await ctx.Ticks(ticks);
        Log(name);
    }

    private static async GameTask Throwing(TaskContext ctx)
    {
        await ctx.NextTick();
        throw new InvalidOperationException("thrown");
    }

    private void Log(string name) => _log.Add($"{name}@{_world.TickIndex}");

    private void Tick(int count)
    {
        for (var i = 0; i < count; i++) _world.Update(Interval);
    }

    private record struct Knock(int Strength);

    private sealed class RunOnce(Action action) : TickSystem
    {
        private bool _ran;

        public override void Tick(in TickContext context)
        {
            if (_ran) return;
            _ran = true;
            action();
        }
    }
}
