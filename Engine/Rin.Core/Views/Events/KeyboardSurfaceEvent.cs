using Rin.Core.Graphics.Windows;
using Rin.Core.Views.Graphics;

namespace Rin.Core.Views.Events;

public class KeyboardSurfaceEvent(ISurface surface, InputKey key, InputState state, InputModifier modifiers = InputModifier.None)
    : SurfaceEvent(surface)
{
    public InputKey Key = key;
    public InputState State = state;
    public InputModifier Modifiers = modifiers;
    public bool IsControlDown => Modifiers.HasFlag(InputModifier.Control);
    public bool IsShiftDown => Modifiers.HasFlag(InputModifier.Shift);
    public bool IsAltDown => Modifiers.HasFlag(InputModifier.Alt);
}