namespace rin.Framework.Graphics.Windows.Events;

public class KeyEvent : Event
{
    public required InputKey Key;
    public required InputState State;
    public required InputModifier Modifiers;
    public bool IsAltDown => Modifiers.HasFlag(InputModifier.Alt);
    public bool IsControlDown => Modifiers.HasFlag(InputModifier.Control);
    public bool IsShiftDown => Modifiers.HasFlag(InputModifier.Shift);
}