using rin.Graphics.Windows;
using rin.Widgets.Graphics;
using rin.Windows;

namespace rin.Widgets.Events;

public class KeyboardEvent(Surface surface,InputKey key,InputState state) : Event(surface)
{
    public InputKey Key = key;
    public InputState State = state;
}