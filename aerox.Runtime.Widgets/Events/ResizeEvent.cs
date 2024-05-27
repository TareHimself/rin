using aerox.Runtime.Math;

namespace aerox.Runtime.Widgets.Events;

public class ResizeEvent : Event
{
    public Vector2<int> Size;

    public ResizeEvent(WidgetSurface surface, Vector2<int> newSize) : base(surface)
    {
        Size = newSize;
    }
}