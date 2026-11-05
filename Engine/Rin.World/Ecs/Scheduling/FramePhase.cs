namespace Rin.World.Ecs.Scheduling;

/// <summary>
///     The phases run once per <see cref="EntityWorld.Update" />, after the ticks, in declaration order.
/// </summary>
public enum FramePhase
{
    Update,
    Late,
    Extract
}
