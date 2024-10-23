using aerox.Runtime.Math;
using aerox.Runtime.Widgets.Graphics;

namespace aerox.Runtime.Widgets.Events;

public class ResizeEvent : Event
{
    public Vector2<int> Size;

    public ResizeEvent(Surface surface, Vector2<int> newSize) : base(surface)
    {
        Size = newSize;
    }
}