using System.Collections.Concurrent;

namespace Rin.Framework.Shared.Threading;

public class TaskPool : IDisposable
{
    private readonly BlockingCollection<PendingTask> _pendingTasks = [];

    private readonly Task[] _tasks;

    public TaskPool(int numThreads)
    {
        _tasks = Enumerable.Range(0, numThreads)
            .Select(_ => Task.Factory.StartNew(RunTask, TaskCreationOptions.LongRunning)).ToArray();
    }

    public TaskPool() : this(Environment.ProcessorCount)
    {
    }

    public void Dispose()
    {
        _pendingTasks.CompleteAdding();
        _pendingTasks.Dispose();

        foreach (var task in _tasks) task.Wait();
    }

    private void RunTask()
    {
        foreach (var pendingTask in _pendingTasks.GetConsumingEnumerable())
            try
            {
                pendingTask.Token?.ThrowIfCancellationRequested();
                pendingTask.Fn();
                pendingTask.Pending.SetResult();
            }
            catch (Exception e)
            {
                pendingTask.Pending.SetException(e);
            }
    }

    public Task Enqueue(Action task, CancellationToken? cancellationToken = null)
    {
        var newPending = new PendingTask(task, new TaskCompletionSource(), cancellationToken);
        _pendingTasks.Add(newPending);
        return newPending.Pending.Task;
    }

    private class PendingTask(Action fn, TaskCompletionSource pending, CancellationToken? token = null)
    {
        public readonly Action Fn = fn;
        public readonly TaskCompletionSource Pending = pending;
        public readonly CancellationToken? Token = token;
    }
}

public class TaskPool<T> : IDisposable
{
    private readonly BlockingCollection<PendingTask> _pendingTasks = [];

    private readonly Task[] _tasks;

    public TaskPool(int numThreads)
    {
        _tasks = Enumerable.Range(0, numThreads)
            .Select(_ => Task.Factory.StartNew(RunTask, TaskCreationOptions.LongRunning)).ToArray();
    }

    public TaskPool() : this(Environment.ProcessorCount)
    {
    }

    public void Dispose()
    {
        _pendingTasks.CompleteAdding();
        _pendingTasks.Dispose();

        foreach (var task in _tasks) task.Wait();
    }


    private void RunTask()
    {
        foreach (var pendingTask in _pendingTasks.GetConsumingEnumerable())
            try
            {
                pendingTask.Token?.ThrowIfCancellationRequested();
                pendingTask.Pending.SetResult(pendingTask.Fn());
            }
            catch (Exception e)
            {
                pendingTask.Pending.SetException(e);
            }
    }

    public Task Enqueue(Func<T> task, CancellationToken? cancellationToken = null)
    {
        var newPending = new PendingTask(task, new TaskCompletionSource<T>(), cancellationToken);
        _pendingTasks.Add(newPending);
        return newPending.Pending.Task;
    }

    public class PendingTask(Func<T> fn, TaskCompletionSource<T> pending, CancellationToken? token = null)
    {
        public readonly CancellationToken? Token = token;
        public Func<T> Fn = fn;
        public TaskCompletionSource<T> Pending = pending;
    }
}