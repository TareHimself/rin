namespace Rin.Engine.Graphics.Windows.Events;

public class CursorButtonEvent : CursorEvent
{
    public required CursorButton Button;
    public required InputModifier Modifiers;
    public required InputState State;
    public bool IsAltDown => Modifiers.HasFlag(InputModifier.Alt);
    public bool IsControlDown => Modifiers.HasFlag(InputModifier.Control);
    public bool IsShiftDown => Modifiers.HasFlag(InputModifier.Shift);
}