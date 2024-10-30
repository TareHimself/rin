using rin.Core.Math;
using rin.Widgets.Graphics;

namespace rin.Widgets.Events;

public class CursorMoveEvent : Event
{
    public Vector2<float> Position;

    public CursorMoveEvent(Surface surface, Vector2<float> position) : base(surface)
    {
        Position = position;
    }
}