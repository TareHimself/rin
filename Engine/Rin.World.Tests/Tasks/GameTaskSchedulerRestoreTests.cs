using Rin.World.Tasks;

namespace Rin.World.Tests.Tasks;

public class GameTaskSchedulerRestoreTests
{
    [Test]
    public void RestoreUnwindsEveryTaskWithRestoredBeforeTheStateIsReplaced()
    {
        var world = new World();
        var tasks = new GameTaskScheduler(world);
        var entity = world.Spawn();
        world.Add(entity, new Health(1));
        var snapshot = new WorldSnapshot();
        world.SaveSnapshot(snapshot);
        world.Get<Health>(entity) = new Health(7);
        var log = new List<string>();
        tasks.Run(entity, async ctx =>
        {
            try
            {
                await ctx.NextTick();
                log.Add("resumed");
            }
            finally
            {
                log.Add($"finally {ctx.CancelReason} health={world.Get<Health>(entity).Value}");
            }
        });

        world.RestoreSnapshot(snapshot);
        world.Update(world.TickInterval);

        Assert.That(log, Is.EqualTo(new[] { "finally Restored health=7" }));
        Assert.That(world.Get<Health>(entity).Value, Is.EqualTo(1));
        Assert.That(tasks.RunningCount, Is.Zero);
    }

    [Test]
    public void AFinallyRunByARestoreCannotLeakChangesIntoTheRestoredWorld()
    {
        var world = new World();
        var tasks = new GameTaskScheduler(world);
        var entity = world.Spawn();
        world.Add(entity, new Health(1));
        var snapshot = new WorldSnapshot();
        world.SaveSnapshot(snapshot);
        var faults = new List<string>();
        tasks.TaskFaulted += (_, exception) => faults.Add(exception.Message);
        tasks.Run(entity, ctx => ChangeInFinally(ctx, () => world.Spawn()));
        tasks.Run(entity, ctx => ChangeInFinally(ctx, () => world.Remove<Health>(entity)));
        tasks.Run(entity, ctx => ChangeInFinally(ctx, () => world.Despawn(entity)));
        tasks.Run(entity, ctx => ChangeInFinally(ctx, () => ctx.Run(async inner => await inner.NextTick())));
        tasks.Run(entity, ctx => ChangeInFinally(ctx, () => world.Commands.Add(entity, new Health(5))));
        tasks.Run(entity, ctx => ChangeInFinally(ctx, () => world.Get<Health>(entity).Value = 99));

        world.RestoreSnapshot(snapshot);
        var restored = new WorldSnapshot();
        world.SaveSnapshot(restored);
        world.Update(world.TickInterval);

        Assert.That(restored.Bytes.ToArray(), Is.EqualTo(snapshot.Bytes.ToArray()));
        Assert.That(faults, Has.Count.EqualTo(4), "the three structural changes and the new task threw");
        Assert.That(world.EntityCount, Is.EqualTo(1));
        Assert.That(world.Get<Health>(entity).Value, Is.EqualTo(1));
        Assert.That(tasks.RunningCount, Is.Zero);
    }

    [Test]
    public void OnlyEventsRaisedAfterARestoreReachWaits()
    {
        var world = new World();
        var tasks = new GameTaskScheduler(world);
        var target = world.Spawn();
        var snapshot = new WorldSnapshot();
        world.SaveSnapshot(snapshot);
        var received = new List<int>();
        tasks.Run(target, ctx => Receive(ctx, target, received));
        tasks.Raise(target, new Ping(1));

        world.RestoreSnapshot(snapshot);
        tasks.Run(target, ctx => Receive(ctx, target, received));
        world.Update(world.TickInterval);
        tasks.Raise(target, new Ping(2));
        world.Update(world.TickInterval);

        Assert.That(received, Is.EqualTo(new[] { 2 }));
    }

    private static async GameTask ChangeInFinally(TaskContext ctx, Action change)
    {
        try
        {
            await ctx.NextTick();
        }
        finally
        {
            change();
        }
    }

    private static async GameTask Receive(TaskContext ctx, Entity target, List<int> received)
    {
        var ping = await ctx.Event<Ping>(target);
        received.Add(ping.Value);
    }

    private readonly record struct Ping(int Value);
}
