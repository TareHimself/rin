using rin.Framework.Core.Math;
using rin.Framework.Widgets.Graphics;

namespace rin.Framework.Widgets.Events;

public class ScrollEvent(Surface surface, Vector2<float> position, Vector2<float> delta)
    : Event(surface)
{
    public Vector2<float> Delta = delta;
    public Vector2<float> Position = position;
}