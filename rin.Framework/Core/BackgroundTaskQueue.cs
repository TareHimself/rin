using System.Collections.Concurrent;

namespace rin.Framework.Core;

public class BackgroundTaskQueue : Disposable
{

    private Task _longTask;
    private Thread _taskThread;
    public class PendingTask
    {
        public Action Fn;
        public TaskCompletionSource Pending;

        public PendingTask(Action fn, TaskCompletionSource pending)
        {
            Fn = fn;
            Pending = pending;
        }
    }

    private readonly BlockingCollection<PendingTask> _pendingTasks = [];
    
    public BackgroundTaskQueue()
    {
        _longTask = Task.Factory.StartNew(() =>
        {
            _taskThread = Thread.CurrentThread;
            
            foreach (var pendingTask in _pendingTasks.GetConsumingEnumerable())
            {
                pendingTask.Fn();
                pendingTask.Pending.SetResult();
            }
            
        },TaskCreationOptions.LongRunning);
    }

    private void RunTask(PendingTask task)
    {
        try
        {
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
        var newPending = new PendingTask(task,new TaskCompletionSource());
        
        if (_taskThread == Thread.CurrentThread)
        {
            RunTask(newPending);
        }
        else
        {
            _pendingTasks.Add(newPending);
        }
        
        return newPending.Pending.Task;
    }

    protected override void OnDispose(bool isManual)
    {
        _pendingTasks.CompleteAdding();
    }
}

public class BackgroundTaskQueue<T> : Disposable
{

    private Task _longTask;
    private Thread? _taskThread;
    public class PendingTask
    {
        public Func<T> Fn;
        public TaskCompletionSource<T> Pending;

        public PendingTask(Func<T> fn, TaskCompletionSource<T> pending)
        {
            Fn = fn;
            Pending = pending;
        }
    }

    private readonly BlockingCollection<PendingTask> _pendingTasks = [];
    
    public BackgroundTaskQueue()
    {
        _longTask = Task.Factory.StartNew(() =>
        {
            _taskThread = Thread.CurrentThread;
            
            foreach (var pendingTask in _pendingTasks.GetConsumingEnumerable())
            {
                pendingTask.Pending.SetResult(pendingTask.Fn());
            }
            
        },TaskCreationOptions.LongRunning);
    }

    private void RunTask(PendingTask task)
    {
        try
        {
            task.Pending.SetResult(task.Fn());
        }
        catch (Exception e)
        {
            task.Pending.SetException(e);
        }
    }
    public Task<T> Enqueue(Func<T> task)
    {
        var newPending = new PendingTask(task,new TaskCompletionSource<T>());
        
        if (_taskThread == Thread.CurrentThread)
        {
            RunTask(newPending);
        }
        else
        {
            _pendingTasks.Add(newPending);
        }
        
        return newPending.Pending.Task;
    }

    protected override void OnDispose(bool isManual)
    {
        _pendingTasks.CompleteAdding();
    }
}