using System.Collections.Concurrent;

namespace Rin.Framework;

public class BackgroundTaskQueue : Disposable
{
    private readonly BlockingCollection<PendingTask> _pendingTasks = [];

    private Task _longTask;
    private Thread _taskThread;

    public BackgroundTaskQueue()
    {
        _taskThread = Thread.CurrentThread;
        _longTask = Task.Factory.StartNew(() =>
        {
            _taskThread = Thread.CurrentThread;

            foreach (var pendingTask in _pendingTasks.GetConsumingEnumerable())
            {
                pendingTask.Fn();
                pendingTask.Pending.SetResult();
            }
        }, TaskCreationOptions.LongRunning);
    }

    private void RunTask(PendingTask task)
    {
        try
        {
            task.Token?.ThrowIfCancellationRequested();
            task.Fn();
            task.Pending.SetResult();
        }
        catch (Exception e)
        {
            task.Pending.SetException(e);
        }
    }

    public Task Enqueue(Action task)
    {
        var newPending = new PendingTask(task, new TaskCompletionSource());

        if (_taskThread == Thread.CurrentThread)
            RunTask(newPending);
        else
            _pendingTasks.Add(newPending);

        return newPending.Pending.Task;
    }

    public Task Enqueue(Action task, CancellationToken cancellationToken)
    {
        var newPending = new PendingTask(task, new TaskCompletionSource(), cancellationToken);


        if (_taskThread == Thread.CurrentThread)
            RunTask(newPending);
        else
            _pendingTasks.Add(newPending, cancellationToken);

        return newPending.Pending.Task;
    }

    protected override void OnDispose(bool isManual)
    {
        _pendingTasks.CompleteAdding();
    }

    private class PendingTask(Action fn, TaskCompletionSource pending, CancellationToken? token = null)
    {
        public readonly Action Fn = fn;
        public readonly TaskCompletionSource Pending = pending;
        public readonly CancellationToken? Token = token;
    }
}

public class BackgroundTaskQueue<T> : Disposable
{
    private readonly BlockingCollection<PendingTask> _pendingTasks = [];

    private Task _longTask;
    private Thread? _taskThread;

    public BackgroundTaskQueue()
    {
        _longTask = Task.Factory.StartNew(() =>
        {
            _taskThread = Thread.CurrentThread;

            foreach (var pendingTask in _pendingTasks.GetConsumingEnumerable())
                pendingTask.Pending.SetResult(pendingTask.Fn());
        }, TaskCreationOptions.LongRunning);
    }

    private void RunTask(PendingTask task)
    {
        try
        {
            task.Token?.ThrowIfCancellationRequested();
            task.Pending.SetResult(task.Fn());
        }
        catch (Exception e)
        {
            task.Pending.SetException(e);
        }
    }

    public Task<T> Enqueue(Func<T> task)
    {
        var newPending = new PendingTask(task, new TaskCompletionSource<T>());

        if (_taskThread == Thread.CurrentThread)
            RunTask(newPending);
        else
            _pendingTasks.Add(newPending);

        return newPending.Pending.Task;
    }

    public Task<T> Enqueue(Func<T> task, CancellationToken cancellationToken)
    {
        var newPending = new PendingTask(task, new TaskCompletionSource<T>(), cancellationToken);


        if (_taskThread == Thread.CurrentThread)
            RunTask(newPending);
        else
            _pendingTasks.Add(newPending, cancellationToken);

        return newPending.Pending.Task;
    }

    protected override void OnDispose(bool isManual)
    {
        _pendingTasks.CompleteAdding();
    }

    public class PendingTask(Func<T> fn, TaskCompletionSource<T> pending, CancellationToken? token = null)
    {
        public readonly CancellationToken? Token = token;
        public Func<T> Fn = fn;
        public TaskCompletionSource<T> Pending = pending;
    }
}