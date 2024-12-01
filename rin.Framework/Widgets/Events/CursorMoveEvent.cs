using rin.Framework.Core.Math;
using rin.Framework.Widgets.Graphics;

namespace rin.Framework.Widgets.Events;

public class CursorMoveEvent(Surface surface, Vector2<float> position) : Event(surface)
{
    public Vector2<float> Position = position;
}