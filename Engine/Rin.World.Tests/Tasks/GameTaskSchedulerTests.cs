using Rin.World.Tasks;

namespace Rin.World.Tests.Tasks;

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
    public void DespawningTheOwnerRunsItsFinallyInTheNextTasksPhaseWithDespawned()
    {
        var (world, tasks) = NewWorld();
        var owner = world.Spawn();
        var log = new List<string>();
        tasks.Run(owner, async ctx =>
        {
            await ctx.NextTick();
            log.Add("first");
            try
            {
                await ctx.Ticks(5);
                log.Add("resumed");
            }
            catch (GameTaskCanceledException e)
            {
                log.Add($"caught {e.Reason}");
                throw;
            }
            finally
            {
                log.Add($"finally {ctx.CancelReason} alive={world.IsAlive(ctx.Owner)}@{world.TickIndex}");
            }
        });

        Tick(world, 1);
        world.Despawn(owner);
        Assert.That(log, Is.EqualTo(new[] { "first" }), "a task not started by a script unwinds in the next Tasks phase");
        Tick(world, 10);

        Assert.That(log, Is.EqualTo(new[] { "first", "caught Despawned", "finally Despawned alive=False@1" }));
        Assert.That(tasks.RunningCount, Is.Zero);
    }

    [Test]
    public void ATaskDespawningAnotherOwnerInTheSamePhaseUnwindsItInThatPhase()
    {
        var (world, tasks) = NewWorld();
        var victim = world.Spawn();
        var log = new List<string>();
        tasks.Run(default, async ctx =>
        {
            await ctx.NextTick();
            world.Despawn(victim);
            log.Add("killer");
        });
        tasks.Run(victim, async ctx =>
        {
            try
            {
                await ctx.NextTick();
                log.Add("victim");
            }
            finally
            {
                log.Add($"victim finally {ctx.CancelReason}@{world.TickIndex}");
            }
        });

        Tick(world, 3);

        Assert.That(log, Is.EqualTo(new[] { "killer", "victim finally Despawned@0" }));
        Assert.That(tasks.RunningCount, Is.Zero);
    }

    [Test]
    public void ExplicitCancelRunsFinallyRightAwayWithCancelled()
    {
        var (world, tasks) = NewWorld();
        var owner = world.Spawn();
        var log = new List<string>();
        tasks.Run(owner, async ctx =>
        {
            try
            {
                await ctx.Ticks(3);
            }
            finally
            {
                log.Add($"finally {ctx.CancelReason}");
            }
        });

        tasks.Cancel(owner);

        Assert.That(log, Is.EqualTo(new[] { "finally Cancelled" }));
        Assert.That(tasks.RunningCount, Is.Zero);
    }

    [Test]
    public void ACancelledTaskThatCatchesAndAwaitsAgainThrowsAgainWithoutWaiting()
    {
        var (world, tasks) = NewWorld();
        var log = new List<string>();
        var checks = 0;
        tasks.Run(world.Spawn(), async ctx =>
        {
            try
            {
                await ctx.Ticks(3);
            }
            catch (GameTaskCanceledException)
            {
                log.Add($"caught cancelled={ctx.IsCancelled}");
            }

            try
            {
                await ctx.Until(() => ++checks > 0);
                log.Add("waited");
            }
            catch (GameTaskCanceledException e)
            {
                log.Add($"again {e.Reason}");
            }

            try
            {
                await ctx.WhenAll(ctx.NextTick(), Child(ctx));
            }
            catch (GameTaskCanceledException e)
            {
                log.Add($"combined {e.Reason}");
            }
        });

        tasks.CancelAll();
        Tick(world, 3);

        Assert.That(log, Is.EqualTo(new[] { "caught cancelled=True", "again Cancelled", "combined Cancelled" }));
        Assert.That(checks, Is.Zero, "the Until wait was never queued, so never checked");
        Assert.That(tasks.RunningCount, Is.Zero);
        return;

        static async GameTask Child(TaskContext ctx) => await ctx.Ticks(2);
    }

    [Test]
    public void AMethodThatReturnsAfterCatchingTheCancelStillReportsCancelledToItsAwaiter()
    {
        var (world, tasks) = NewWorld();
        var log = new List<string>();
        tasks.Run(default, async ctx =>
        {
            try
            {
                log.Add($"result {await Swallowing(ctx)}");
            }
            catch (GameTaskCanceledException e)
            {
                log.Add($"awaiter {e.Reason}");
            }
        });

        tasks.CancelAll();
        Tick(world, 2);

        Assert.That(log, Is.EqualTo(new[] { "swallowed", "awaiter Cancelled" }));
        return;

        async GameTask<int> Swallowing(TaskContext ctx)
        {
            try
            {
                await ctx.NextTick();
            }
            catch (GameTaskCanceledException)
            {
                log.Add("swallowed");
            }

            return 5;
        }
    }

    [Test]
    public void ACancellationIsNotAFault()
    {
        var (world, tasks) = NewWorld();
        var owner = world.Spawn();
        var swallowed = false;
        tasks.Run(owner, async ctx => await ctx.Ticks(5));
        tasks.Run(owner, async ctx =>
        {
            try
            {
                await ctx.Ticks(5);
            }
            catch (GameTaskCanceledException)
            {
                swallowed = true;
            }
        });
        tasks.Run(default, async ctx =>
        {
            await ctx.NextTick();
            throw new GameTaskCanceledException(CancelReason.Cancelled);
        });

        world.Despawn(owner);

        Assert.That(() => Tick(world, 2), Throws.Nothing, "with no TaskFaulted subscriber a fault would be rethrown");
        Assert.That(swallowed, Is.True);
        Assert.That(tasks.RunningCount, Is.Zero);
    }

    [Test]
    public void AnExceptionThrownFromFinallyWhileCancellingIsAFault()
    {
        var (world, tasks) = NewWorld();
        var owner = world.Spawn();
        var faults = new List<(Entity, string)>();
        tasks.TaskFaulted += (entity, exception) => faults.Add((entity, exception.Message));
        tasks.Run(owner, async ctx =>
        {
            try
            {
                await ctx.WhenAny(ctx.Ticks(3), Child(ctx));
            }
            catch (InvalidOperationException e)
            {
                faults.Add((default, $"caught {e.Message}"));
            }
            finally
            {
                throw new InvalidOperationException("finally");
            }
        });

        tasks.Cancel(owner);
        Assert.That(faults, Is.EqualTo(new[] { (default(Entity), "caught child finally") }),
            "the child's fault reaches the await instead of the cancel, and the task fault waits for the Tasks phase");
        Tick(world, 1);

        Assert.That(faults, Is.EqualTo(new[] { (default(Entity), "caught child finally"), (owner, "finally") }));
        return;

        static async GameTask Child(TaskContext ctx)
        {
            try
            {
                await ctx.Ticks(2);
            }
            finally
            {
                throw new InvalidOperationException("child finally");
            }
        }
    }

    [Test]
    public void AFaultFromFinallyWhileCancellingIsRethrownFromUpdateWithoutASubscriber()
    {
        var (world, tasks) = NewWorld();
        tasks.Run(default, async ctx =>
        {
            try
            {
                await ctx.NextTick();
            }
            finally
            {
                throw new InvalidOperationException("finally");
            }
        });

        tasks.CancelAll();

        Assert.That(() => world.Update(Interval), Throws.InvalidOperationException.With.Message.EqualTo("finally"));
    }

    [Test]
    public void TwoIdenticalWorldsCancelInTheSameOrder()
    {
        var first = RunCancelScenario();
        Assert.That(first, Is.EqualTo(RunCancelScenario()));
        Assert.That(first, Is.EqualTo(new[] { "0 Despawned", "1 Despawned", "2 Despawned", "3 Despawned" }),
            "tasks unwind in start order, not in the order their waits were queued or their owners despawned");
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

        static void RunFrames(World world, GameTaskScheduler tasks, Entity door, int frames)
        {
            for (var i = 0; i < frames; i++)
            {
                if (i % 3 == 0) tasks.Raise(door, new Knock(i % 2));
                world.Update(Interval);
            }
        }
    }

    private static List<string> RunCancelScenario()
    {
        var (world, tasks) = NewWorld();
        var log = new List<string>();
        var owners = new Entity[4];
        for (var i = 0; i < owners.Length; i++) owners[i] = world.Spawn();
        for (var i = 0; i < owners.Length; i++)
        {
            var index = i;
            tasks.Run(owners[i], async ctx =>
            {
                try
                {
                    await ctx.Ticks(owners.Length - index);
                    await ctx.Ticks(10);
                }
                finally
                {
                    log.Add($"{index} {ctx.CancelReason}");
                }
            });
        }

        Tick(world, 4);
        for (var i = owners.Length - 1; i >= 0; i--) world.Despawn(owners[i]);
        Tick(world, 1);
        return log;
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

    private static (World World, GameTaskScheduler Tasks) NewWorld()
    {
        var world = new World { TickInterval = Interval };
        return (world, new GameTaskScheduler(world));
    }

    private static void Tick(World world, int count)
    {
        for (var i = 0; i < count; i++) world.Update(Interval);
    }

    private record struct Knock(int Strength);
}
