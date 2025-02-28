using Rin.Engine.Graphics.Windows;
using Rin.Engine.Views.Graphics;

namespace Rin.Engine.Views.Events;

public class KeyboardEvent(Surface surface, InputKey key, InputState state) : Event(surface)
{
    public InputKey Key = key;
    public InputState State = state;
}