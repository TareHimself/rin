namespace Rin.World.Scheduling;

/// <summary>
///     The phases of one fixed tick, run in declaration order.
/// </summary>
public enum TickPhase
{
    Input,
    Gameplay,
    Tasks,
    Physics
}
