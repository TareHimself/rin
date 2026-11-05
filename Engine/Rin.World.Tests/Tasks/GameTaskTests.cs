using Rin.World.Tasks;

namespace Rin.World.Tests.Tasks;

public class GameTaskTests
{
    private const float Interval = 0.125f;

    private World _world = null!;
    private GameTaskScheduler _tasks = null!;

    [SetUp]
    public void SetUp()
    {
        _world = new World { TickInterval = Interval };
        _tasks = new GameTaskScheduler(_world);
    }

    [Test]
    public void DefaultIsCompleted()
    {
        Assert.That(default(GameTask).IsCompleted, Is.True);
        Assert.That(default(GameTask<int>).IsCompleted, Is.True);
    }

    [Test]
    public void ResultsFlowFromSuspendedAndSynchronousChildren()
    {
        var results = new List<int>();
        _tasks.Run(default, async ctx =>
        {
            results.Add(await Delayed(ctx, 7));
            results.Add(await Immediate(3));
        });

        Tick(3);

        Assert.That(results, Is.EqualTo(new[] { 7, 3 }));
        Assert.That(_tasks.RunningCount, Is.Zero);
        return;

        static async GameTask<int> Delayed(TaskContext ctx, int value)
        {
            await ctx.Ticks(2);
            return value;
        }

#pragma warning disable CS1998
        static async GameTask<int> Immediate(int value) => value;
#pragma warning restore CS1998
    }

    [Test]
    public void AChildsExceptionIsRethrownAtTheAwait()
    {
        var caught = new List<string>();
        _tasks.Run(default, async ctx =>
        {
            try
            {
                await Throwing(ctx);
            }
            catch (InvalidOperationException e)
            {
                caught.Add(e.Message);
            }
        });

        Tick(2);

        Assert.That(caught, Is.EqualTo(new[] { "child" }));
        return;

        static async GameTask Throwing(TaskContext ctx)
        {
            await ctx.NextTick();
            throw new InvalidOperationException("child");
        }
    }

    [Test]
    public void AwaitingATaskInsideAGameTaskFaultsIt()
    {
        var faults = new List<Exception>();
        _tasks.TaskFaulted += (_, exception) => faults.Add(exception);
        _tasks.Run(default, async _ => await Task.Delay(1));

        Tick(1);

        Assert.That(faults, Has.Count.EqualTo(1));
        Assert.That(faults[0], Is.InstanceOf<InvalidOperationException>());
    }

    [Test]
    public void AGameTaskCanBeAwaitedOnlyOnce()
    {
        var faults = new List<Exception>();
        _tasks.TaskFaulted += (_, exception) => faults.Add(exception);
        _tasks.Run(default, async ctx =>
        {
            var wait = ctx.NextTick();
            await wait;
            await wait;
        });

        Tick(2);

        Assert.That(faults, Has.Count.EqualTo(1));
        Assert.That(faults[0], Is.InstanceOf<InvalidOperationException>());
    }

    private void Tick(int count)
    {
        for (var i = 0; i < count; i++) _world.Update(Interval);
    }
}
