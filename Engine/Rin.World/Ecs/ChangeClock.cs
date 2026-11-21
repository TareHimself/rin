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

    public void Advance() => Current++;
}
