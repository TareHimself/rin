using System.Collections.Concurrent;

namespace Rin.Core.Shared;

/// <summary>
///     Used to schedule actions to be called on <see cref="DispatchPending" /> . useful for threading
/// </summary>
public class Dispatcher
{
    private readonly ConcurrentQueue<IScheduledItem> _actions = new();

    /// <summary>
    ///     Resolve scheduled tasks
    /// </summary>
    public void DispatchPending()
    {
        while (_actions.TryDequeue(out var action))
        {
            action.Execute();
        }
    }

    /// <summary>
    ///     Schedule an action to be run on <see cref="DispatchPending" />
    /// </summary>
    /// <param name="action"></param>
    /// <returns></returns>
    public Task Enqueue(Action action)
    {
        return Enqueue(action, CancellationToken.None);
    }

    /// <summary>
    ///     Schedule an action to be run on <see cref="DispatchPending" />
    /// </summary>
    /// <param name="action"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task Enqueue(Action action, CancellationToken cancellationToken)
    {
        return Enqueue(static cb => cb(), action, cancellationToken);
    }

    public Task Enqueue<TState>(Action<TState> action, TState state)
    {
        return Enqueue(action, state, CancellationToken.None);
    }

    public Task Enqueue<TState>(Action<TState> action, TState state, CancellationToken cancellationToken)
    {
        var completionSource = new TaskCompletionSource();
        _actions.Enqueue(new Scheduled<TState>(action, state, cancellationToken, completionSource));
        return completionSource.Task;
    }

    interface IScheduledItem
    {
        void Execute();
    }

    private class Scheduled<TState> : IScheduledItem
    {
        private readonly Action<TState> _pendingAction;
        private readonly TState _state;
        private readonly CancellationToken _cancellationToken;
        private readonly TaskCompletionSource _completionSource;

        public Scheduled(Action<TState> pendingAction, TState state, CancellationToken cancellationToken,
            TaskCompletionSource completionSource)
        {
            _pendingAction = pendingAction;
            _state = state;
            _cancellationToken = cancellationToken;
            _completionSource = completionSource;
        }

        public void Execute()
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _completionSource.TrySetCanceled(_cancellationToken);
                return;
            }

            try
            {
                _pendingAction(_state);
                _completionSource.TrySetResult();
            }
            catch (Exception e)
            {
                _completionSource.TrySetException(e);
            }
        }
    }
}