using System.Collections.Concurrent;

namespace Rin.Framework.Shared;

/// <summary>
///  Used to schedule actions to be called on <see cref="DispatchPending" /> . usefull for threading
/// </summary>
public class Dispatcher
{
    private readonly ConcurrentQueue<Scheduled> _actions = new();

    /// <summary>
    ///     Resolve scheduled tasks
    /// </summary>
    public void DispatchPending()
    {
        while (_actions.TryDequeue(out var action))
        {
            if (action.CancellationToken is { IsCancellationRequested: true }) continue;

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
    }

    /// <summary>
    ///     Schedule an action to be run on <see cref="DispatchPending" />
    /// </summary>
    /// <param name="action"></param>
    /// <returns></returns>
    public Task Enqueue(Action action)
    {
        var pending = new Scheduled
        {
            PendingAction = action
        };
        
        _actions.Enqueue(pending);

        return pending.CompletionSource.Task;
    }

    /// <summary>
    ///     Schedule an action to be run on <see cref="DispatchPending" />
    /// </summary>
    /// <param name="action"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task Enqueue(Action action, CancellationToken cancellationToken)
    {
        var pending = new Scheduled
        {
            PendingAction = action,
            CancellationToken = cancellationToken
        };
        
        _actions.Enqueue(pending);

        return pending.CompletionSource.Task;
    }

    private class Scheduled
    {
        public readonly TaskCompletionSource CompletionSource = new();
        public CancellationToken? CancellationToken;
        public required Action PendingAction;
    }
}