using Rin.World.Tasks;

namespace Rin.World.Tests.Tasks;

public class GameTaskSchedulerRestoreTests
{
    [Test]
    public void RestoreCancelsEveryTask()
    {
        var world = new World();
        var tasks = new GameTaskScheduler(world);
        var resumed = false;
        var snapshot = new WorldSnapshot();
        world.SaveSnapshot(snapshot);
        tasks.Run(default, async ctx =>
        {
            await ctx.NextTick();
            resumed = true;
        });

        world.RestoreSnapshot(snapshot);
        world.Update(world.TickInterval);

        Assert.That(resumed, Is.False);
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

    private static async GameTask Receive(TaskContext ctx, Entity target, List<int> received)
    {
        var ping = await ctx.Event<Ping>(target);
        received.Add(ping.Value);
    }

    private readonly record struct Ping(int Value);
}
