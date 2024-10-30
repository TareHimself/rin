using rin.Core.Math;
using rin.Widgets.Graphics;

namespace rin.Widgets.Events;

public class ResizeEvent : Event
{
    public Vector2<int> Size;

    public ResizeEvent(Surface surface, Vector2<int> newSize) : base(surface)
    {
        Size = newSize;
    }
}