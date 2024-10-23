using aerox.Runtime.Math;
using aerox.Runtime.Widgets.Graphics;

namespace aerox.Runtime.Widgets.Events;

public class CursorMoveEvent : Event
{
    public Vector2<float> Position;

    public CursorMoveEvent(Surface surface, Vector2<float> position) : base(surface)
    {
        Position = position;
    }
}