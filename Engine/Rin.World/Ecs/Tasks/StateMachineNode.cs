using System.Runtime.CompilerServices;

namespace Rin.World.Ecs.Tasks;

/// <summary>
///     The node of an <c>async GameTask</c> method, completed by its builder.
/// </summary>
internal abstract class AsyncNode<TResult> : TaskNode<TResult>
{
    public void SetResult(TResult result)
    {
        Result = result;
        Succeed();
    }

    public void SetException(Exception exception) => Fail(exception);
}

/// <summary>
///     Holds a suspended state machine, rented on its first suspension and resumed when the awaited node completes.
/// </summary>
internal sealed class StateMachineNode<TStateMachine, TResult> : AsyncNode<TResult>
    where TStateMachine : IAsyncStateMachine
{
    private TaskNode? _awaited;

    public TStateMachine StateMachine = default!;

    public override void AwaitDependency(TaskNode dependency)
    {
        if (dependency.Status == TaskNodeStatus.Cancelled)
        {
            dependency.Release();
            Cancel();
            return;
        }

        dependency.EnsureNotAwaited();
        _awaited = dependency;
        dependency.Parent = this;
    }

    public override void OnDependencyCompleted(TaskNode dependency)
    {
        _awaited = null;
        if (dependency.Status == TaskNodeStatus.Cancelled)
        {
            dependency.Release();
            Cancel();
            return;
        }

        StateMachine.MoveNext();
    }

    protected override void CancelDependencies()
    {
        var awaited = _awaited;
        _awaited = null;
        Drop(awaited);
    }

    protected override void ResetState()
    {
        base.ResetState();
        StateMachine = default!;
        _awaited = null;
    }

    protected override void ReturnToPool() => NodePool<StateMachineNode<TStateMachine, TResult>>.Return(this);
}

/// <summary>
///     Carries an exception thrown before an <c>async GameTask</c> method first suspended.
/// </summary>
internal sealed class FaultedNode<TResult> : TaskNode<TResult>
{
    public FaultedNode(Exception exception) => Fail(exception);

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
        if (node is null) node = new FaultedNode<TResult>(exception);
        else ((AsyncNode<TResult>)node).SetException(exception);
    }

    public static void SetResult<TResult>(TaskNode? node, TResult result)
    {
        if (node is not null) ((AsyncNode<TResult>)node).SetResult(result);
    }
}
