using Rin.Core.Shared.Threading;

namespace Rin.Core.Tests.Shared.Threading;

public class BackgroundTaskQueueTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Test]
    public void TasksRunInOrderOnTheQueueThread()
    {
        using var queue = new BackgroundTaskQueue();
        var order = new List<int>();
        var threads = new HashSet<int>();

        Task last = Task.CompletedTask;
        for (var i = 0; i < 8; i++)
        {
            var index = i;
            last = queue.Enqueue(() =>
            {
                order.Add(index);
                threads.Add(Environment.CurrentManagedThreadId);
            });
        }

        Assert.That(last.Wait(Timeout), Is.True);
        Assert.That(order, Is.EqualTo(Enumerable.Range(0, 8)));
        Assert.That(threads, Has.Count.EqualTo(1));
        Assert.That(threads, Does.Not.Contain(Environment.CurrentManagedThreadId));
    }

    [Test]
    public void AThrowingTaskFaultsItsTaskAndTheQueueKeepsRunning()
    {
        using var queue = new BackgroundTaskQueue();

        var failed = queue.Enqueue(() => throw new InvalidOperationException("boom"));
        var next = queue.Enqueue(() => { });

        Assert.That(next.Wait(Timeout), Is.True);
        Assert.That(failed.Exception?.InnerException, Is.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void ATaskCancelledWhileQueuedDoesNotRun()
    {
        using var queue = new BackgroundTaskQueue();
        using var gate = new ManualResetEventSlim();
        using var cancellation = new CancellationTokenSource();
        var ran = false;
        queue.Enqueue(gate.Wait);

        var cancelled = queue.Enqueue(() => ran = true, cancellation.Token);
        cancellation.Cancel();
        gate.Set();

        Assert.That(queue.Enqueue(() => { }).Wait(Timeout), Is.True);
        Assert.That(cancelled.Exception?.InnerException, Is.InstanceOf<OperationCanceledException>());
        Assert.That(ran, Is.False);
    }

    [Test]
    public void EnqueueingFromTheQueueThreadRunsTheTaskInline()
    {
        using var queue = new BackgroundTaskQueue();
        var innerCompletedInline = false;

        var outer = queue.Enqueue(() => innerCompletedInline = queue.Enqueue(() => { }).IsCompleted);

        Assert.That(outer.Wait(Timeout), Is.True);
        Assert.That(innerCompletedInline, Is.True);
    }

    [Test]
    public void DisposeFinishesTheQueuedTasks()
    {
        var queue = new BackgroundTaskQueue();
        using var gate = new ManualResetEventSlim();
        queue.Enqueue(gate.Wait);
        var queued = queue.Enqueue(() => { });

        gate.Set();
        queue.Dispose();

        Assert.That(queued.IsCompletedSuccessfully, Is.True);
    }

    [Test]
    public void ATypedQueueReturnsTheResultAndFaultsOnAThrow()
    {
        using var queue = new BackgroundTaskQueue<int>();

        var failed = queue.Enqueue(() => throw new InvalidOperationException("boom"));
        var value = queue.Enqueue(() => 42);

        Assert.That(value.Wait(Timeout), Is.True);
        Assert.That(value.Result, Is.EqualTo(42));
        Assert.That(failed.Exception?.InnerException, Is.TypeOf<InvalidOperationException>());
    }
}
