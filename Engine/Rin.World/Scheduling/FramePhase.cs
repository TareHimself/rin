namespace Rin.World.Scheduling;

/// <summary>
///     The phases run once per <see cref="World.Update" />, after the ticks, in declaration order.
/// </summary>
public enum FramePhase
{
    Update,
    Late,
    Extract
}
