using Rin.Core.Graphics.Windows;
using Rin.Core.Views.Graphics;

namespace Rin.Core.Views.Events;

public class KeyboardSurfaceEvent(ISurface surface, InputKey key, InputState state) : SurfaceEvent(surface)
{
    public InputKey Key = key;
    public InputState State = state;
}