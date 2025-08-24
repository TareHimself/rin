using Rin.Framework.Graphics.Windows;
using Rin.Framework.Views.Graphics;

namespace Rin.Framework.Views.Events;

public class KeyboardSurfaceEvent(Surface surface, InputKey key, InputState state) : SurfaceEvent(surface)
{
    public InputKey Key = key;
    public InputState State = state;
}