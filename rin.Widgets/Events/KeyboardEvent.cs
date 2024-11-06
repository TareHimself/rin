using rin.Widgets.Graphics;
using rin.Windows;

namespace rin.Widgets.Events;

public class KeyboardEvent(Surface surface,Key key,KeyState state) : Event(surface)
{
    public Key Key = key;
    public KeyState State = state;
}