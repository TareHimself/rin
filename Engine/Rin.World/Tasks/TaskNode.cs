using System.Runtime.ExceptionServices;
using Rin.World.Scripting;

namespace Rin.World.Tasks;

internal enum TaskNodeStatus : byte
{
    Pending,
    Succeeded,
    Faulted,
    Cancelled
}

/// <summary>
///     The pooled source behind a <see cref="GameTask" />. A node is released back to its pool once its result is
///     taken, and its <see cref="Version" /> changes so stale <see cref="GameTask" /> copies are detected.
/// </summary>
/// <remarks>
///     Completion flows up to <see cref="Parent" />, the node awaiting this one. Cancellation flows down to the nodes
///     this one awaits and then up like a completion, but an awaiting state machine is never resumed by it.
/// </remarks>
internal abstract class TaskNode
{
    private ExceptionDispatchInfo? _error;
    private GameTaskScheduler? _rootScheduler;

    public short Version { get; private set; }

    public TaskNodeStatus Status { get; private set; }

    public TaskNode? Parent { get; set; }

    public Entity Owner { get; set; }

    public Script? Script { get; set; }

    /// <summary>
    ///     When the node was queued or started, the tiebreak for nodes due in the same phase.
    /// </summary>
    public long Sequence { get; set; }

    public bool IsRoot => _rootScheduler is not null;

    public bool IsPending(short version) => version == Version && Status == TaskNodeStatus.Pending;

    public bool IsCompleted(short version)
    {
        Validate(version);
        return Status is TaskNodeStatus.Succeeded or TaskNodeStatus.Faulted;
    }

    public void Validate(short version)
    {
        if (version != Version)
            throw new InvalidOperationException("This GameTask was already awaited. A GameTask can be awaited once.");
    }

    public void MarkRoot(GameTaskScheduler scheduler)
    {
        EnsureNotAwaited();
        _rootScheduler = scheduler;
    }

    public void EnsureNotAwaited()
    {
        if (Parent is not null || IsRoot)
            throw new InvalidOperationException("This GameTask is already awaited. A GameTask can be awaited once.");
    }

    /// <summary>
    ///     Rethrows a fault, then releases the node.
    /// </summary>
    public void Consume(short version)
    {
        Validate(version);
        switch (Status)
        {
            case TaskNodeStatus.Succeeded:
                Release();
                return;
            case TaskNodeStatus.Faulted:
                var error = TakeError();
                Release();
                error.Throw();
                return;
            default:
                throw new InvalidOperationException("This GameTask has not completed.");
        }
    }

    public ExceptionDispatchInfo TakeError()
    {
        var error = _error ?? throw new InvalidOperationException("This GameTask did not fault.");
        _error = null;
        return error;
    }

    public void Cancel()
    {
        if (Status != TaskNodeStatus.Pending) return;
        Status = TaskNodeStatus.Cancelled;
        CancelDependencies();
        NotifyCompleted();
    }

    public void Release()
    {
        Version++;
        Status = TaskNodeStatus.Pending;
        _error = null;
        _rootScheduler = null;
        Parent = null;
        Owner = default;
        Script = null;
        Sequence = 0;
        ResetState();
        ReturnToPool();
    }

    /// <summary>
    ///     Called by a dependency this node awaits once that dependency completes, in any state.
    /// </summary>
    public virtual void OnDependencyCompleted(TaskNode dependency) =>
        throw new InvalidOperationException($"{GetType().Name} does not await other tasks.");

    /// <summary>
    ///     Makes this node await <paramref name="dependency" />. Only state machine nodes await.
    /// </summary>
    public virtual void AwaitDependency(TaskNode dependency) =>
        throw new InvalidOperationException($"{GetType().Name} does not await other tasks.");

    /// <summary>
    ///     Whether a queued wait can complete in the current phase. Only waits queued on a scheduler override this.
    /// </summary>
    public virtual bool IsDue(GameTaskScheduler scheduler) => false;

    public virtual void CompleteDue() => Succeed();

    protected void Succeed()
    {
        Status = TaskNodeStatus.Succeeded;
        NotifyCompleted();
    }

    protected void Fail(ExceptionDispatchInfo error)
    {
        _error = error;
        Status = TaskNodeStatus.Faulted;
        NotifyCompleted();
    }

    protected void Fail(Exception exception) => Fail(ExceptionDispatchInfo.Capture(exception));

    /// <summary>
    ///     Detaches and cancels whatever this node awaits.
    /// </summary>
    protected virtual void CancelDependencies()
    {
    }

    protected virtual void ResetState()
    {
    }

    protected abstract void ReturnToPool();

    /// <summary>
    ///     Cancels and releases a dependency this node no longer waits for.
    /// </summary>
    protected static void Drop(TaskNode? dependency)
    {
        if (dependency is null) return;
        dependency.Parent = null;
        dependency.Cancel();
        dependency.Release();
    }

    private void NotifyCompleted()
    {
        if (Parent is { } parent)
        {
            Parent = null;
            parent.OnDependencyCompleted(this);
        }
        else
        {
            _rootScheduler?.OnRootCompleted(this);
        }
    }
}

internal abstract class TaskNode<TResult> : TaskNode
{
    public TResult Result { get; protected set; } = default!;

    public TResult ConsumeResult(short version)
    {
        Validate(version);
        if (Status != TaskNodeStatus.Succeeded)
        {
            Consume(version);
            return default!;
        }

        var result = Result;
        Release();
        return result;
    }

    protected override void ResetState() => Result = default!;
}

/// <summary>
///     A <see cref="TaskNode" /> with no result value.
/// </summary>
internal readonly struct NoResult;

/// <summary>
///     Per thread, so worlds ticking on different threads never share a pool.
/// </summary>
internal static class NodePool<TNode> where TNode : TaskNode, new()
{
    [ThreadStatic] private static Stack<TNode>? _free;

    public static TNode Rent() => _free is { Count: > 0 } free ? free.Pop() : new TNode();

    public static void Return(TNode node) => (_free ??= new Stack<TNode>()).Push(node);
}
