using aerox.Runtime.Math;
using aerox.Runtime.Widgets.Graphics;
using aerox.Runtime.Windows;

namespace aerox.Runtime.Widgets.Events;

public class CursorUpEvent : Event
{
    public Vector2<float> Position;
    public MouseButton Button;

    public CursorUpEvent(Surface surface,MouseButton button, Vector2<float> position) : base(surface)
    {
        Button = button;
        Position = position;
    }
}