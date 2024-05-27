using aerox.Runtime.Math;

namespace aerox.Runtime.Widgets.Events;

public class CursorMoveEvent : Event
{
    public Vector2<float> Position;

    public CursorMoveEvent(WidgetSurface surface, Vector2<float> position) : base(surface)
    {
        Position = position;
    }
}