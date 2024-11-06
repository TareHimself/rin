using rin.Core.Math;
using rin.Widgets.Graphics;

namespace rin.Widgets.Events;

public class CursorMoveEvent(Surface surface, Vector2<float> position) : Event(surface)
{
    public Vector2<float> Position = position;
}