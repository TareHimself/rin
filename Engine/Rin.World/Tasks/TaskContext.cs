using Rin.World.Scripting;

namespace Rin.World.Tasks;

/// <summary>
///     Starts tasks and makes the waits they await. Every wait made here is owned by <see cref="Owner" /> and is
///     cancelled when it is despawned, or when the <see cref="Scripting.Script" /> that started it stops.
/// </summary>
public readonly struct TaskContext
{
    internal TaskContext(GameTaskScheduler scheduler, Entity owner, Script? script)
    {
        Scheduler = scheduler;
        Owner = owner;
        Script = script;
    }

    public GameTaskScheduler Scheduler => field ?? throw new InvalidOperationException("This TaskContext has no scheduler.");

    /// <summary>
    ///     In the <c>finally</c> of a task cancelled with <see cref="Tasks.CancelReason.Despawned" />, it may already be
    ///     gone: <see cref="World.IsAlive" /> is false for it unless a script's task is unwinding inside the despawn.
    /// </summary>
    public Entity Owner { get; }

    internal Script? Script { get; }

    /// <summary>
    ///     True in the <c>catch</c> and <c>finally</c> blocks of a cancelled task, and in the code it runs from them.
    /// </summary>
    public bool IsCancelled => CancellationScope.Current is not null;

    /// <summary>
    ///     Why the running task was cancelled, or <c>null</c> when it was not. See <see cref="IsCancelled" />.
    /// </summary>
    public CancelReason? CancelReason => CancellationScope.Current;

    /// <summary>
    ///     Starts another task with the same owner. See <see cref="GameTaskScheduler.Run" />.
    /// </summary>
    public void Run(Func<TaskContext, GameTask> body) => Scheduler.Run(Owner, Script, body);

    /// <summary>
    ///     Resumes in the <paramref name="ticks" />-th Tasks phase from now. Awaited before the Tasks phase of a tick,
    ///     <c>Ticks(1)</c> resumes later in that same tick. Zero or less is already complete.
    /// </summary>
    public GameTask Ticks(int ticks) =>
        ticks <= 0 ? default : Scheduler.Enqueue(TickWait.Rent(Scheduler.Phase + ticks), this);

    public GameTask NextTick() => Ticks(1);

    /// <summary>
    ///     <see cref="Ticks" /> for <paramref name="seconds" /> rounded up to whole ticks of
    ///     <see cref="World.TickInterval" />.
    /// </summary>
    public GameTask Seconds(float seconds) => Ticks(TicksFor(seconds, Scheduler.World.TickInterval));

    /// <summary>
    ///     Completes in the first Tasks phase where <paramref name="condition" /> returns true, checked once per phase.
    ///     An exception from it faults the wait.
    /// </summary>
    public GameTask Until(Func<bool> condition) => Scheduler.Enqueue(UntilWait.Rent(condition), this);

    /// <summary>
    ///     Completes with the first <typeparamref name="T" /> raised for <paramref name="target" /> with
    ///     <see cref="GameTaskScheduler.Raise{T}" /> after this call.
    /// </summary>
    public GameTask<T> Event<T>(Entity target) where T : unmanaged => Scheduler.Enqueue(EventWait<T>.Rent(target), this);

    /// <summary>
    ///     Completes with 0 or 1, the index of the first task to complete, and cancels the other one with
    ///     <see cref="Tasks.CancelReason.Cancelled" />. If that task faulted or was cancelled, so is this one.
    /// </summary>
    public GameTask<int> WhenAny(GameTask first, GameTask second) =>
        new(WhenAnyNode.Start(first.TakeNode(), second.TakeNode()));

    /// <summary>
    ///     Completes when both tasks have. If either faults or is cancelled, the other is cancelled and so is this one.
    /// </summary>
    public GameTask WhenAll(GameTask first, GameTask second) =>
        new(WhenAllNode.Start(first.TakeNode(), second.TakeNode()));

    public void Raise<T>(Entity target, in T value) where T : unmanaged => Scheduler.Raise(target, value);

    internal static int TicksFor(float seconds, float tickInterval)
    {
        // The tolerance keeps float noise such as 0.3f / 0.1f = 3.0000001 from adding a whole tick.
        const double tolerance = 1e-4;
        return seconds <= 0f ? 0 : (int)System.Math.Ceiling(seconds / (double)tickInterval - tolerance);
    }
}
