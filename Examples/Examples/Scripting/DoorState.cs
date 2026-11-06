using Rin.World;

namespace Examples.Scripting;

/// <summary>
///     The <see cref="SlidingDoor" />'s progress, in a component so a snapshot saves it.
/// </summary>
public struct DoorState : IComponentData
{
    public bool IsOpen;
}
