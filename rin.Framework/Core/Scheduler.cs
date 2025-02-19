namespace rin.Framework.Core;

public class Scheduler
{
    private readonly Queue<Scheduled> _actions = [];

    private readonly object _lock = new();

    /// <summary>
    ///     Resolve scheduled tasks
    /// </summary>
    public void Update()
    {
        Queue<Scheduled> actions;
        lock (_lock)
        {
            actions = new Queue<Scheduled>(_actions);
            _actions.Clear();
        }

        foreach (var action in actions)
            try
            {
                action.PendingAction.Invoke();
                action.CompletionSource.SetResult();
            }
            catch (Exception e)
            {
                action.CompletionSource.SetException(e);
            }
    }

    /// <summary>
    ///     Schedule an action to be run on <see cref="Update" />
    /// </summary>
    /// <param name="action"></param>
    /// <returns></returns>
    public Task Schedule(Action action)
    {
        var pending = new Scheduled
        {
            PendingAction = action
        };
        lock (_lock)
        {
            _actions.Enqueue(pending);
        }

        return pending.CompletionSource.Task;
    }

    private class Scheduled
    {
        public readonly TaskCompletionSource CompletionSource = new();
        public required Action PendingAction;
    }
}