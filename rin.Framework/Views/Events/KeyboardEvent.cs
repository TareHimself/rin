using rin.Framework.Graphics.Windows;
using rin.Framework.Views.Graphics;

namespace rin.Framework.Views.Events;

public class KeyboardEvent(Surface surface, InputKey key, InputState state) : Event(surface)
{
    public InputKey Key = key;
    public InputState State = state;
}