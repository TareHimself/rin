using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Rin.World.Tasks;

/// <summary>
///     Builds <c>async GameTask</c> methods. The method runs synchronously until its first suspension, which rents a
///     pooled node for the state machine. No <see cref="Task" />, thread pool or synchronization context is involved.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public struct GameTaskMethodBuilder
{
    private TaskNode? _node;

    public static GameTaskMethodBuilder Create() => default;

    public readonly GameTask Task => _node is null ? default : new GameTask(_node);

    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine =>
        stateMachine.MoveNext();

    public void SetStateMachine(IAsyncStateMachine stateMachine)
    {
    }

    public readonly void SetResult() => GameTaskBuilderCore.SetResult<NoResult>(_node, default);

    public void SetException(Exception exception) => GameTaskBuilderCore.SetException<NoResult>(ref _node, exception);

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine =>
        GameTaskBuilderCore.AwaitOnCompleted<TAwaiter, TStateMachine, NoResult>(ref _node, ref awaiter, ref stateMachine);

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine =>
        GameTaskBuilderCore.AwaitOnCompleted<TAwaiter, TStateMachine, NoResult>(ref _node, ref awaiter, ref stateMachine);
}

/// <summary>
///     Builds <c>async GameTask&lt;T&gt;</c> methods, like <see cref="GameTaskMethodBuilder" />.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public struct GameTaskMethodBuilder<T>
{
    private TaskNode? _node;
    private T _result;

    public static GameTaskMethodBuilder<T> Create() => default;

    public readonly GameTask<T> Task => _node is null ? new GameTask<T>(_result) : new GameTask<T>((TaskNode<T>)_node);

    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine =>
        stateMachine.MoveNext();

    public void SetStateMachine(IAsyncStateMachine stateMachine)
    {
    }

    public void SetResult(T result)
    {
        if (_node is null) _result = result;
        else GameTaskBuilderCore.SetResult(_node, result);
    }

    public void SetException(Exception exception) => GameTaskBuilderCore.SetException<T>(ref _node, exception);

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine =>
        GameTaskBuilderCore.AwaitOnCompleted<TAwaiter, TStateMachine, T>(ref _node, ref awaiter, ref stateMachine);

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine =>
        GameTaskBuilderCore.AwaitOnCompleted<TAwaiter, TStateMachine, T>(ref _node, ref awaiter, ref stateMachine);
}
