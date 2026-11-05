namespace Rin.World.Tasks;

/// <summary>
///     The cancel reason of the task whose code is running on this thread, or <c>null</c>. A cancelled task never
///     suspends again, so all of its code after the cancel runs inside its resume.
/// </summary>
internal static class CancellationScope
{
    [ThreadStatic] private static CancelReason? _current;

    public static CancelReason? Current => _current;

    /// <returns>
    ///     The reason to restore with <see cref="Exit" />.
    /// </returns>
    public static CancelReason? Enter(CancelReason? reason)
    {
        var outer = _current;
        _current = reason;
        return outer;
    }

    public static void Exit(CancelReason? outer) => _current = outer;
}
