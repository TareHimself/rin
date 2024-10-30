using rin.Core.Math;
using rin.Widgets.Graphics;

namespace rin.Widgets.Events;

public class ScrollEvent : Event
{
    public Vector2<float> Delta;
    public Vector2<float> Position;

    public ScrollEvent(Surface surface, Vector2<float> position, Vector2<float> delta) : base(surface)
    {
        Position = position;
        Delta = delta;
    }
}