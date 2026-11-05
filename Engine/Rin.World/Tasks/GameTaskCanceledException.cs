namespace Rin.World.Tasks;

/// <summary>
///     Thrown from the pending <c>await</c> of a cancelled <see cref="GameTask" />, and from every later one, so
///     <c>catch</c>, <c>finally</c> and <c>using</c> run. Escaping a task, it cancels the task instead of faulting it.
/// </summary>
public sealed class GameTaskCanceledException(CancelReason reason)
    : OperationCanceledException($"The GameTask was cancelled ({reason}).")
{
    public CancelReason Reason { get; } = reason;
}
