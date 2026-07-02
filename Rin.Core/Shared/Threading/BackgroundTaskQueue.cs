using System.Collections.Concurrent;
using JetBrains.Annotations;

namespace Rin.Core.Shared.Threading;

public class BackgroundTaskQueue : IDisposable
{
    private readonly BlockingCollection<PendingTask> _pendingTasks = [];
    private readonly Thread _taskThread;

    [PublicAPI]
    public string Name
    {
        get;
        init
        {
            field = value;
            _taskThread.Name = field;
        }
    } = "Background Task Queue";

    public BackgroundTaskQueue()
    {
        _taskThread = new Thread(ProcessTasks)
        {
            IsBackground = true,
            Priority = ThreadPriority.BelowNormal,
        };
        _taskThread.Start();
    }


    private void ProcessTasks()
    {
        foreach (var pendingTask in _pendingTasks.GetConsumingEnumerable())
        {
            pendingTask.Fn();
            pendingTask.Pending.SetResult();
        }
    }

    public void Dispose()
    {
        _pendingTasks.CompleteAdding();
        _taskThread.Join();
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

    private class PendingTask(Action fn, TaskCompletionSource pending, CancellationToken? token = null)
    {
        public readonly Action Fn = fn;
        public readonly TaskCompletionSource Pending = pending;
        public readonly CancellationToken? Token = token;
    }
}

public class BackgroundTaskQueue<T> : IDisposable
{
    private readonly BlockingCollection<PendingTask> _pendingTasks = [];
    private readonly Thread _taskThread;

    [PublicAPI] public string Name { get; set; } = "Background Task Queue";

    public BackgroundTaskQueue()
    {
        _taskThread = new Thread(ProcessTasks)
        {
            IsBackground = true,
            Name = Name,
            Priority = ThreadPriority.BelowNormal,
        };
        _taskThread.Start();
    }


    private void ProcessTasks()
    {
        foreach (var pendingTask in _pendingTasks.GetConsumingEnumerable())
            pendingTask.Pending.SetResult(pendingTask.Fn());
    }

    public void Dispose()
    {
        _pendingTasks.CompleteAdding();
        _taskThread.Join();
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

    private class PendingTask(Func<T> fn, TaskCompletionSource<T> pending, CancellationToken? token = null)
    {
        public readonly CancellationToken? Token = token;
        public Func<T> Fn = fn;
        public readonly TaskCompletionSource<T> Pending = pending;
    }
}