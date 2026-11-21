namespace Rin.World.Ecs;

/// <summary>
///     The ticks one reader of change ticks compares rows against: a row changed when its tick is newer than
///     <see cref="Since" />, the tick of the reader's previous run.
/// </summary>
/// <remarks>
///     A run is every use at one <see cref="ChangeClock" /> tick, so a system that iterates the same query twice sees the
///     same changes both times, and its own writes, stamped with its run tick, are not newer on its next run.
/// </remarks>
internal struct ChangeWindow
{
    private uint _runTick;

    public uint Since { get; private set; }

    public void BeginRun(uint currentTick)
    {
        if (currentTick == _runTick) return;
        Since = _runTick;
        _runTick = currentTick;
    }
}
