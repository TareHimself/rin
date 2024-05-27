using aerox.Runtime.Math;

namespace aerox.Runtime.Widgets.Events;

public class CursorDownEvent : Event
{
    public Vector2<float> Position;

    public CursorDownEvent(WidgetSurface surface, Vector2<float> position) : base(surface)
    {
        Position = position;
    }
}