namespace rin.Framework.Graphics.Windows.Events;

public class CursorButtonEvent : CursorEvent
{
    public required CursorButton Button;
    public required InputState State;
    public required InputModifier Modifiers;
    public bool IsAltDown => Modifiers.HasFlag(InputModifier.Alt);
    public bool IsControlDown => Modifiers.HasFlag(InputModifier.Control);
    public bool IsShiftDown => Modifiers.HasFlag(InputModifier.Shift);
}