using rin.Core.Math;
using rin.Windows.Native.src;

namespace rin.Graphics.Windows.Events;

public class CharacterEvent : Event
{
    public required char Data;
    public required InputModifier Modifiers;
    public bool IsAltDown => Modifiers.HasFlag(InputModifier.Alt);
    public bool IsControlDown => Modifiers.HasFlag(InputModifier.Control);
    public bool IsShiftDown => Modifiers.HasFlag(InputModifier.Shift);
}