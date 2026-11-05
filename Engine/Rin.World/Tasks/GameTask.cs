using System.Runtime.CompilerServices;

namespace Rin.World.Tasks;

/// <summary>
///     A single threaded task resumed only in <see cref="Scheduling.TickPhase.Tasks" />. Await it once, inside another
///     <c>async GameTask</c> method, or start it with <see cref="TaskContext.Run" />. <c>default</c> is a completed task.
/// </summary>
[AsyncMethodBuilder(typeof(GameTaskMethodBuilder))]
public readonly struct GameTask
{
    private readonly TaskNode? _node;
    private readonly short _version;

    internal GameTask(TaskNode node)
    {
        _node = node;
        _version = node.Version;
    }

    internal GameTask(TaskNode? node, short version)
    {
        _node = node;
        _version = version;
    }

    public bool IsCompleted => _node is null || _node.IsCompleted(_version);

    public GameTaskAwaiter GetAwaiter() => new(_node, _version);

    /// <summary>
    ///     The node, checked against stale copies, or <c>null</c> for a task that completed synchronously.
    /// </summary>
    internal TaskNode? TakeNode()
    {
        _node?.Validate(_version);
        return _node;
    }
}

/// <summary>
///     A <see cref="GameTask" /> with a result. Converts to <see cref="GameTask" /> to pass to
///     <see cref="TaskContext.WhenAny" /> or <see cref="TaskContext.WhenAll" />, which drops the result.
/// </summary>
[AsyncMethodBuilder(typeof(GameTaskMethodBuilder<>))]
public readonly struct GameTask<T>
{
    private readonly TaskNode<T>? _node;
    private readonly short _version;
    private readonly T _result;

    internal GameTask(TaskNode<T> node)
    {
        _node = node;
        _version = node.Version;
        _result = default!;
    }

    internal GameTask(T result)
    {
        _node = null;
        _version = 0;
        _result = result;
    }

    public bool IsCompleted => _node is null || _node.IsCompleted(_version);

    public GameTaskAwaiter<T> GetAwaiter() => new(_node, _version, _result);

    public static implicit operator GameTask(GameTask<T> task) => new(task._node, task._version);
}

public readonly struct GameTaskAwaiter : ICriticalNotifyCompletion
{
    private readonly TaskNode? _node;
    private readonly short _version;

    static GameTaskAwaiter() => AwaiterAccess<GameTaskAwaiter>.Registered = new Access();

    internal GameTaskAwaiter(TaskNode? node, short version)
    {
        _node = node;
        _version = version;
    }

    /// <summary>
    ///     False for a cancelled task too, so the awaiting method suspends and is cancelled instead of resuming.
    /// </summary>
    public bool IsCompleted => _node is null || _node.IsCompleted(_version);

    public void GetResult() => _node?.Consume(_version);

    public void OnCompleted(Action continuation) => throw GameTaskAwaiting.OutsideGameTask();

    public void UnsafeOnCompleted(Action continuation) => throw GameTaskAwaiting.OutsideGameTask();

    private sealed class Access : AwaiterAccess<GameTaskAwaiter>
    {
        public override void AwaitBy(ref GameTaskAwaiter awaiter, TaskNode parent) =>
            GameTaskAwaiting.AwaitBy(awaiter._node, awaiter._version, parent);
    }
}

public readonly struct GameTaskAwaiter<T> : ICriticalNotifyCompletion
{
    private readonly TaskNode<T>? _node;
    private readonly short _version;
    private readonly T _result;

    static GameTaskAwaiter() => AwaiterAccess<GameTaskAwaiter<T>>.Registered = new Access();

    internal GameTaskAwaiter(TaskNode<T>? node, short version, T result)
    {
        _node = node;
        _version = version;
        _result = result;
    }

    /// <summary>
    ///     False for a cancelled task too, so the awaiting method suspends and is cancelled instead of resuming.
    /// </summary>
    public bool IsCompleted => _node is null || _node.IsCompleted(_version);

    public T GetResult() => _node is null ? _result : _node.ConsumeResult(_version);

    public void OnCompleted(Action continuation) => throw GameTaskAwaiting.OutsideGameTask();

    public void UnsafeOnCompleted(Action continuation) => throw GameTaskAwaiting.OutsideGameTask();

    private sealed class Access : AwaiterAccess<GameTaskAwaiter<T>>
    {
        public override void AwaitBy(ref GameTaskAwaiter<T> awaiter, TaskNode parent) =>
            GameTaskAwaiting.AwaitBy(awaiter._node, awaiter._version, parent);
    }
}

internal static class GameTaskAwaiting
{
    public static void AwaitBy(TaskNode? node, short version, TaskNode parent)
    {
        if (node is null) throw new InvalidOperationException("A completed GameTask has nothing to wait for.");
        node.Validate(version);
        parent.AwaitDependency(node);
    }

    public static InvalidOperationException OutsideGameTask() =>
        new("A GameTask can only be awaited inside an async GameTask method, so it never resumes on a thread pool.");
}
