namespace Rin.World.Ecs;

/// <summary>
///     The tick every component write is stamped with. It only goes up: once before and once after every system run, so
///     a system's writes carry a tick that no other code shares, and once for each snapshot restore.
/// </summary>
/// <remarks>
///     It starts at 1 so a query that has never run, whose last run is 0, sees every row as changed. Wrapping after
///     2^32 advances is not handled.
/// </remarks>
internal sealed class ChangeClock
{
    public uint Current { get; private set; } = 1;

    /// <summary>
    ///     True while the fixed tick runs, where change queries throw: ticks are not saved, so a change query would see
    ///     different rows after a rollback.
    /// </summary>
    public bool IsInTickPhase { get; private set; }

    public void Advance() => Current++;

    public TickPhaseScope EnterTickPhase()
    {
        IsInTickPhase = true;
        return new TickPhaseScope(this);
    }

    public void EnsureCanRunChangeQuery()
    {
        if (IsInTickPhase)
            throw new InvalidOperationException(
                "A Changed query cannot run during the tick: change ticks are not saved, so it would see different rows after a rollback. Run it in a frame system.");
    }

    /// <summary>
    ///     Marks the fixed tick as running until disposed.
    /// </summary>
    public readonly ref struct TickPhaseScope(ChangeClock clock)
    {
        public void Dispose() => clock.IsInTickPhase = false;
    }
}
