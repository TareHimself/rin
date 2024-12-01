using rin.Framework.Graphics.Windows;
using rin.Framework.Widgets.Graphics;


namespace rin.Framework.Widgets.Events;

public class KeyboardEvent(Surface surface,InputKey key,InputState state) : Event(surface)
{
    public InputKey Key = key;
    public InputState State = state;
}