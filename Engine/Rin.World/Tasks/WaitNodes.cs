using System.Runtime.ExceptionServices;

namespace Rin.World.Tasks;

internal sealed class TickWait : TaskNode
{
    private long _duePhase;

    public static TickWait Rent(long duePhase)
    {
        var wait = NodePool<TickWait>.Rent();
        wait._duePhase = duePhase;
        return wait;
    }

    public override bool IsDue(GameTaskScheduler scheduler) => scheduler.Phase >= _duePhase;

    protected override void ResetState() => _duePhase = 0;

    protected override void ReturnToPool() => NodePool<TickWait>.Return(this);
}

internal sealed class UntilWait : TaskNode
{
    private Func<bool>? _condition;
    private Exception? _conditionError;

    public static UntilWait Rent(Func<bool> condition)
    {
        var wait = NodePool<UntilWait>.Rent();
        wait._condition = condition;
        return wait;
    }

    public override bool IsDue(GameTaskScheduler scheduler)
    {
        try
        {
            return _condition!();
        }
        catch (Exception exception)
        {
            _conditionError = exception;
            return true;
        }
    }

    public override void CompleteDue()
    {
        if (_conditionError is { } error) Fail(error);
        else Succeed();
    }

    protected override void ResetState()
    {
        _condition = null;
        _conditionError = null;
    }

    protected override void ReturnToPool() => NodePool<UntilWait>.Return(this);
}

internal sealed class EventWait<T> : TaskNode<T> where T : unmanaged
{
    private Entity _target;
    private T _received;

    public static EventWait<T> Rent(Entity target)
    {
        var wait = NodePool<EventWait<T>>.Rent();
        wait._target = target;
        return wait;
    }

    public override bool IsDue(GameTaskScheduler scheduler) =>
        scheduler.Channel<T>().TryFind(_target, Sequence, scheduler.EventCutoff, out _received);

    public override void CompleteDue()
    {
        Result = _received;
        Succeed();
    }

    protected override void ResetState()
    {
        base.ResetState();
        _target = default;
        _received = default;
    }

    protected override void ReturnToPool() => NodePool<EventWait<T>>.Return(this);
}

/// <summary>
///     Completes with the index of the first dependency to complete and cancels the other. A fault, from the winner or
///     from the loser unwinding, wins over a result or a cancel.
/// </summary>
internal sealed class WhenAnyNode : TaskNode<int>
{
    private TaskNode? _first;
    private TaskNode? _second;

    public static WhenAnyNode Start(TaskNode? first, TaskNode? second)
    {
        var node = NodePool<WhenAnyNode>.Rent();
        if (first is null || first.Status != TaskNodeStatus.Pending) node.Settle(first, second, 0);
        else if (second is null || second.Status != TaskNodeStatus.Pending) node.Settle(second, first, 1);
        else node.Attach(first, second);
        return node;
    }

    public override void OnDependencyCompleted(TaskNode dependency)
    {
        var index = dependency == _first ? 0 : 1;
        var other = index == 0 ? _second : _first;
        _first = _second = null;
        Settle(dependency, other, index);
    }

    protected override void OnCancelRequested(CancelReason reason)
    {
        var first = _first;
        var second = _second;
        _first = _second = null;
        var firstError = Drop(first, reason);
        var secondError = Drop(second, reason);
        CompleteDropped(firstError ?? secondError, reason);
    }

    protected override void ResetState()
    {
        base.ResetState();
        _first = _second = null;
    }

    protected override void ReturnToPool() => NodePool<WhenAnyNode>.Return(this);

    private void Attach(TaskNode first, TaskNode second)
    {
        first.EnsureNotAwaited();
        second.EnsureNotAwaited();
        _first = first;
        _second = second;
        first.Parent = this;
        second.Parent = this;
    }

    private void Settle(TaskNode? winner, TaskNode? loser, int index)
    {
        var winnerReason = winner?.CancelReason ?? Tasks.CancelReason.Cancelled;
        var loserError = Drop(loser, winnerReason);
        switch (winner?.Status)
        {
            case TaskNodeStatus.Faulted:
                var error = winner.TakeError();
                winner.Release();
                Fail(error);
                break;
            case TaskNodeStatus.Cancelled:
                winner.Release();
                CompleteDropped(loserError, winnerReason);
                break;
            default:
                winner?.Release();
                if (loserError is not null)
                {
                    Fail(loserError);
                    break;
                }

                Result = index;
                Succeed();
                break;
        }
    }

    private void CompleteDropped(ExceptionDispatchInfo? error, CancelReason reason)
    {
        if (error is not null) Fail(error);
        else CompleteCancelled(reason);
    }
}

/// <summary>
///     Completes when both dependencies succeed. A fault or a cancellation of either cancels the other and passes on;
///     a fault, from either side, wins over a cancel.
/// </summary>
internal sealed class WhenAllNode : TaskNode
{
    private TaskNode? _first;
    private TaskNode? _second;

    public static WhenAllNode Start(TaskNode? first, TaskNode? second)
    {
        var node = NodePool<WhenAllNode>.Rent();
        node._first = first;
        node._second = second;
        node.WatchIfPending(first);
        node.WatchIfPending(second);
        node.SettleIfCompleted(first);
        node.SettleIfCompleted(second);
        if (node is { _first: null, _second: null, Status: TaskNodeStatus.Pending }) node.Succeed();
        return node;
    }

    public override void OnDependencyCompleted(TaskNode dependency)
    {
        var isFirst = dependency == _first;
        if (isFirst) _first = null;
        else _second = null;

        if (dependency.Status == TaskNodeStatus.Succeeded)
        {
            dependency.Release();
            if (_first is null && _second is null) Succeed();
            return;
        }

        var other = isFirst ? _second : _first;
        _first = _second = null;
        if (dependency.Status == TaskNodeStatus.Faulted)
        {
            var error = dependency.TakeError();
            dependency.Release();
            Drop(other, Tasks.CancelReason.Cancelled);
            Fail(error);
            return;
        }

        var reason = dependency.CancelReason!.Value;
        dependency.Release();
        CompleteDropped(Drop(other, reason), reason);
    }

    protected override void OnCancelRequested(CancelReason reason)
    {
        var first = _first;
        var second = _second;
        _first = _second = null;
        var firstError = Drop(first, reason);
        var secondError = Drop(second, reason);
        CompleteDropped(firstError ?? secondError, reason);
    }

    protected override void ResetState()
    {
        _first = _second = null;
    }

    protected override void ReturnToPool() => NodePool<WhenAllNode>.Return(this);

    private void WatchIfPending(TaskNode? dependency)
    {
        if (dependency is not { Status: TaskNodeStatus.Pending }) return;
        dependency.EnsureNotAwaited();
        dependency.Parent = this;
    }

    // A dependency settled earlier may have dropped this one already, so it must still be tracked.
    private void SettleIfCompleted(TaskNode? dependency)
    {
        if (dependency is null || Status != TaskNodeStatus.Pending) return;
        if (dependency != _first && dependency != _second) return;
        if (dependency.Status != TaskNodeStatus.Pending) OnDependencyCompleted(dependency);
    }

    private void CompleteDropped(ExceptionDispatchInfo? error, CancelReason reason)
    {
        if (error is not null) Fail(error);
        else CompleteCancelled(reason);
    }
}
