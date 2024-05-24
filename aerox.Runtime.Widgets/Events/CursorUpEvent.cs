using aerox.Runtime.Math;

namespace aerox.Runtime.Widgets.Events;

public class CursorUpEvent : Event
{
    public Vector2<float> Position;
    
    public CursorUpEvent(WidgetSurface surface, Vector2<float> position) : base(surface)
    {
        Position = position;

    }
}