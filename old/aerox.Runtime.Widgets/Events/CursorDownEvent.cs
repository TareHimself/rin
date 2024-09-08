using aerox.Runtime.Math;
using aerox.Runtime.Windows;

namespace aerox.Runtime.Widgets.Events;

public class CursorDownEvent : Event
{
    public Vector2<float> Position;
    public MouseButton Button;

    public CursorDownEvent(WidgetSurface surface,MouseButton button, Vector2<float> position) : base(surface)
    {
        Button = button;
        Position = position;
    }
}