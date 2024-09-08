using aerox.Runtime.Math;

namespace aerox.Runtime.Widgets.Events;

public class ScrollEvent : Event
{
    public Vector2<float> Delta;
    public Vector2<float> Position;

    public ScrollEvent(WidgetSurface surface, Vector2<float> position, Vector2<float> delta) : base(surface)
    {
        Position = position;
        Delta = delta;
    }
}