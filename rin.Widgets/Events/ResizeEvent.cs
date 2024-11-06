using rin.Core.Math;
using rin.Widgets.Graphics;

namespace rin.Widgets.Events;

public class ResizeEvent(Surface surface, Vector2<int> newSize) : Event(surface)
{
    public Vector2<int> Size = newSize;
}