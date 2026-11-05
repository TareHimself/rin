using Rin.World.Ecs;
using Rin.World.Ecs.Tasks;

namespace Rin.World.Tests.Ecs.Tasks;

public class GameTaskSchedulerTests
{
    private const float Interval = 0.125f;

    [Test]
    public void WaitsDueInTheSamePhaseResumeInTheOrderTheyWereQueued()
    {
        var (world, tasks) = NewWorld();
        var log = new List<string>();
        tasks.Run(default, async ctx =>
        {
            await ctx.Ticks(3);
            log.Add("a");
        });
        tasks.Run(default, async ctx =>
        {
            await ctx.Ticks(1);
            await ctx.Ticks(2);
            log.Add("b");
        });
        tasks.Run(default, async ctx =>
        {
            await ctx.Ticks(3);
            log.Add("c");
        });

        Tick(world, 3);

        Assert.That(log, Is.EqualTo(new[] { "a", "c", "b" }), "b queued its second wait after a and c queued theirs");
    }

    [Test]
    public void TwoIdenticalWorldsResumeInTheSameOrder()
    {
        Assert.That(RunScenario(), Is.EqualTo(RunScenario()));
        Assert.That(RunScenario(), Has.Count.GreaterThan(12));
    }

    [Test]
    public void DespawningTheOwnerCancelsItsTasksWithoutResumingThem()
    {
        var (world, tasks) = NewWorld();
        var owner = world.Spawn();
        var resumed = new List<string>();
        tasks.Run(owner, async ctx =>
        {
            await ctx.NextTick();
            resumed.Add("first");
            try
            {
                await ctx.Ticks(5);
            }
            finally
            {
                resumed.Add("finally");
            }
        });

        Tick(world, 1);
        world.Despawn(owner);
        Tick(world, 10);

        Assert.That(resumed, Is.EqualTo(new[] { "first" }), "a cancelled task stops silently, finally blocks do not run");
        Assert.That(tasks.RunningCount, Is.Zero);
    }

    [Test]
    public void ATaskDespawningAnotherOwnerInTheSamePhaseStopsItFromResuming()
    {
        var (world, tasks) = NewWorld();
        var victim = world.Spawn();
        var resumed = new List<string>();
        tasks.Run(default, async ctx =>
        {
            await ctx.NextTick();
            world.Despawn(victim);
        });
        tasks.Run(victim, async ctx =>
        {
            await ctx.NextTick();
            resumed.Add("victim");
        });

        Tick(world, 3);

        Assert.That(resumed, Is.Empty);
        Assert.That(tasks.RunningCount, Is.Zero);
    }

    [Test]
    public void CancelByOwnerStopsOnlyThatOwnersTasks()
    {
        var (world, tasks) = NewWorld();
        var first = world.Spawn();
        var second = world.Spawn();
        var resumed = new List<Entity>();
        foreach (var owner in new[] { first, second })
            tasks.Run(owner, async ctx =>
            {
                await ctx.NextTick();
                resumed.Add(ctx.Owner);
            });

        tasks.Cancel(first);
        Tick(world, 2);

        Assert.That(resumed, Is.EqualTo(new[] { second }));
    }

    [Test]
    public void CancelAllStopsEveryTask()
    {
        var (world, tasks) = NewWorld();
        var resumed = 0;
        for (var i = 0; i < 3; i++)
            tasks.Run(default, async ctx =>
            {
                await ctx.WhenAll(ctx.NextTick(), ctx.Until(() => true));
                resumed++;
            });

        tasks.CancelAll();
        Tick(world, 3);

        Assert.That(resumed, Is.Zero);
        Assert.That(tasks.RunningCount, Is.Zero);
    }

    [Test]
    public void AFaultedTaskIsReportedWithItsOwnerAtTheEndOfTheTasksPhase()
    {
        var (world, tasks) = NewWorld();
        var owner = world.Spawn();
        var faults = new List<(Entity, string)>();
        var resumedAfter = false;
        tasks.TaskFaulted += (entity, exception) => faults.Add((entity, exception.Message));
        tasks.Run(owner, async ctx =>
        {
            await ctx.NextTick();
            throw new InvalidOperationException("boom");
        });
        tasks.Run(default, async ctx =>
        {
            await ctx.NextTick();
            resumedAfter = true;
        });

        Tick(world, 1);

        Assert.That(faults, Is.EqualTo(new[] { (owner, "boom") }));
        Assert.That(resumedAfter, Is.True, "a fault does not stop the other tasks of the phase");
    }

    [Test]
    public void AFaultWithNoSubscriberIsRethrownFromUpdate()
    {
        var (world, tasks) = NewWorld();
        tasks.Run(default, async ctx =>
        {
            await ctx.NextTick();
            throw new InvalidOperationException("boom");
        });

        Assert.That(() => world.Update(Interval), Throws.InvalidOperationException.With.Message.EqualTo("boom"));
        Assert.That(() => world.Update(Interval), Throws.Nothing, "a fault is reported once");
    }

    [Test]
    public void AnExceptionBeforeTheFirstAwaitIsReportedInTheNextTasksPhase()
    {
        var (world, tasks) = NewWorld();
        var faults = new List<string>();
        tasks.TaskFaulted += (_, exception) => faults.Add(exception.Message);

        tasks.Run(default, Faulting);
        Assert.That(faults, Is.Empty);

        Tick(world, 1);
        Assert.That(faults, Is.EqualTo(new[] { "before the first suspension" }));
        return;

        static async GameTask Faulting(TaskContext ctx)
        {
            await ctx.Ticks(0);
            throw new InvalidOperationException("before the first suspension");
        }
    }

    [Test]
    public void SteadyStateAwaitingDoesNotAllocate()
    {
        var (world, tasks) = NewWorld();
        var door = world.Spawn();
        var open = false;
        Func<bool> isOpen = () => open;
        tasks.Run(door, async ctx =>
        {
            while (true)
            {
                await ctx.NextTick();
                await ctx.Until(isOpen);
                await ctx.WhenAny(ctx.Ticks(2), ctx.Event<Knock>(door));
                await ctx.WhenAll(ctx.NextTick(), ctx.Ticks(2));
            }
        });
        tasks.Run(default, async ctx =>
        {
            while (true)
            {
                var knock = await ctx.Event<Knock>(door);
                open = knock.Strength > 0;
            }
        });

        RunFrames(world, tasks, door, 50);
        var before = GC.GetAllocatedBytesForCurrentThread();
        RunFrames(world, tasks, door, 200);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.That(allocated, Is.Zero);
        return;

        static void RunFrames(EntityWorld world, GameTaskScheduler tasks, Entity door, int frames)
        {
            for (var i = 0; i < frames; i++)
            {
                if (i % 3 == 0) tasks.Raise(door, new Knock(i % 2));
                world.Update(Interval);
            }
        }
    }

    private static List<string> RunScenario()
    {
        var (world, tasks) = NewWorld();
        var log = new List<string>();
        var entities = new Entity[4];
        for (var i = 0; i < entities.Length; i++) entities[i] = world.Spawn();
        for (var i = 0; i < entities.Length; i++)
        {
            var index = i;
            tasks.Run(entities[i], async ctx =>
            {
                for (var step = 0; step < 3; step++)
                {
                    var winner = await ctx.WhenAny(ctx.Ticks(1 + (index + step) % 3), ctx.Event<Knock>(ctx.Owner));
                    log.Add($"{index}:{step}:{winner}@{world.TickIndex}");
                    await ctx.Until(() => world.TickIndex % 2 == index % 2);
                    log.Add($"{index}:until@{world.TickIndex}");
                }
            });
        }

        for (var frame = 0; frame < 20; frame++)
        {
            if (frame % 4 == 1) tasks.Raise(entities[frame % entities.Length], new Knock(frame));
            world.Update(Interval);
        }

        return log;
    }

    private static (EntityWorld World, GameTaskScheduler Tasks) NewWorld()
    {
        var world = new EntityWorld { TickInterval = Interval };
        return (world, new GameTaskScheduler(world));
    }

    private static void Tick(EntityWorld world, int count)
    {
        for (var i = 0; i < count; i++) world.Update(Interval);
    }

    private record struct Knock(int Strength);
}
