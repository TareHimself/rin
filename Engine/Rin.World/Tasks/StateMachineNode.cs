using System.Runtime.CompilerServices;

namespace Rin.World.Tasks;

/// <summary>
///     The node of an <c>async GameTask</c> method, completed by its builder. Once cancelled it ends cancelled, even
///     when the method catches the <see cref="GameTaskCanceledException" /> and returns.
/// </summary>
internal abstract class AsyncNode<TResult> : TaskNode<TResult>
{
    public void SetResult(TResult result)
    {
        if (CancelReason is { } reason)
        {
            CompleteCancelled(reason);
            return;
        }

        Result = result;
        Succeed();
    }

    public void SetException(Exception exception)
    {
        if (exception is GameTaskCanceledException cancelled) CompleteCancelled(CancelReason ?? cancelled.Reason);
        else Fail(exception);
    }
}

/// <summary>
///     Holds a suspended state machine, rented on its first suspension and resumed when the awaited node completes.
/// </summary>
/// <remarks>
///     A cancelled method is resumed with the exception at its pending <c>await</c>, and every later <c>await</c>
///     cancels what it awaits so it throws right away. Those resumes happen inside <see cref="Resume" />, which loops
///     instead of recursing so a method that keeps catching does not grow the stack.
/// </remarks>
internal sealed class StateMachineNode<TStateMachine, TResult> : AsyncNode<TResult>
    where TStateMachine : IAsyncStateMachine
{
    private TaskNode? _awaited;
    private bool _running;
    private bool _resumeAgain;

    public TStateMachine StateMachine = default!;

    public override void AwaitDependency(TaskNode dependency)
    {
        dependency.EnsureNotAwaited();
        _awaited = dependency;
        if (dependency.Status != TaskNodeStatus.Pending)
        {
            OnDependencyCompleted(dependency);
            return;
        }

        dependency.Parent = this;
        if (CancelReason is { } reason) dependency.Cancel(reason);
    }

    public override void OnDependencyCompleted(TaskNode dependency)
    {
        _awaited = null;
        if (dependency.Status == TaskNodeStatus.Cancelled) MarkCancelled(dependency.CancelReason!.Value);
        Resume();
    }

    protected override void OnCancelRequested(CancelReason reason) => _awaited?.Cancel(reason);

    protected override void ResetState()
    {
        base.ResetState();
        StateMachine = default!;
        _awaited = null;
        _running = false;
        _resumeAgain = false;
    }

    protected override void ReturnToPool() => NodePool<StateMachineNode<TStateMachine, TResult>>.Return(this);

    private void Resume()
    {
        if (_running)
        {
            _resumeAgain = true;
            return;
        }

        // Completing releases the node back to the pool, which bumps the version; it must not be touched after that.
        var version = Version;
        _running = true;
        do
        {
            _resumeAgain = false;
            var outer = CancellationScope.Enter(CancelReason);
            try
            {
                StateMachine.MoveNext();
            }
            finally
            {
                CancellationScope.Exit(outer);
            }
        } while (Version == version && _resumeAgain);

        if (Version == version) _running = false;
    }
}

/// <summary>
///     Carries an exception thrown before an <c>async GameTask</c> method first suspended: cancelled for a
///     <see cref="GameTaskCanceledException" />, faulted for any other.
/// </summary>
internal sealed class ThrownNode<TResult> : TaskNode<TResult>
{
    public ThrownNode(Exception exception)
    {
        if (exception is GameTaskCanceledException cancelled) CompleteCancelled(cancelled.Reason);
        else Fail(exception);
    }

    protected override void ReturnToPool()
    {
    }
}

/// <summary>
///     Attaches an awaiter of type <typeparamref name="TAwaiter" /> to the node awaiting it. Only the awaiters of
///     <see cref="GameTask" /> and <see cref="GameTask{T}" /> register one, from their static constructors.
/// </summary>
/// <remarks>
///     An interface cast on the awaiter would box it in unoptimized and tier 0 code, so every first await would allocate.
/// </remarks>
internal abstract class AwaiterAccess<TAwaiter>
{
    public static AwaiterAccess<TAwaiter>? Registered { get; set; }

    public abstract void AwaitBy(ref TAwaiter awaiter, TaskNode parent);
}

internal static class GameTaskBuilderCore
{
    public static void AwaitOnCompleted<TAwaiter, TStateMachine, TResult>(ref TaskNode? node, ref TAwaiter awaiter,
        ref TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine
    {
        var access = AwaiterAccess<TAwaiter>.Registered ?? throw new InvalidOperationException(
            $"A GameTask method can only await GameTask and GameTask<T>, not {typeof(TAwaiter).Name}.");

        if (node is null)
        {
            var box = NodePool<StateMachineNode<TStateMachine, TResult>>.Rent();
            // node aliases the builder inside stateMachine, so it is set before the copy into the box.
            node = box;
            box.StateMachine = stateMachine;
        }

        access.AwaitBy(ref awaiter, node);
    }

    public static void SetException<TResult>(ref TaskNode? node, Exception exception)
    {
        if (node is null) node = new ThrownNode<TResult>(exception);
        else ((AsyncNode<TResult>)node).SetException(exception);
    }

    public static void SetResult<TResult>(TaskNode? node, TResult result)
    {
        if (node is not null) ((AsyncNode<TResult>)node).SetResult(result);
    }
}
