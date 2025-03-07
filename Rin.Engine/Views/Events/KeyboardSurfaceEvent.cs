using Rin.Engine.Graphics.Windows;
using Rin.Engine.Views.Graphics;

namespace Rin.Engine.Views.Events;

public class KeyboardSurfaceEvent(Surface surface, InputKey key, InputState state) : SurfaceEvent(surface)
{
    public InputKey Key = key;
    public InputState State = state;
}