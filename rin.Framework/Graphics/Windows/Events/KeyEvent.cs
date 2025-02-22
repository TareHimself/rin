namespace rin.Framework.Graphics.Windows.Events;

public class KeyEvent : WindowEvent
{
    public required InputKey Key;
    public required InputModifier Modifiers;
    public required InputState State;
    public bool IsAltDown => Modifiers.HasFlag(InputModifier.Alt);
    public bool IsControlDown => Modifiers.HasFlag(InputModifier.Control);
    public bool IsShiftDown => Modifiers.HasFlag(InputModifier.Shift);
}