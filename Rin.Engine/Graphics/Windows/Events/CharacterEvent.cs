namespace Rin.Engine.Graphics.Windows.Events;

public class CharacterEvent : WindowEvent
{
    public required char Data;
    public required InputModifier Modifiers;
    public bool IsAltDown => Modifiers.HasFlag(InputModifier.Alt);
    public bool IsControlDown => Modifiers.HasFlag(InputModifier.Control);
    public bool IsShiftDown => Modifiers.HasFlag(InputModifier.Shift);
}