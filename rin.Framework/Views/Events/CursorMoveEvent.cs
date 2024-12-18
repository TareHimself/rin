using rin.Framework.Core.Math;
using rin.Framework.Views.Graphics;

namespace rin.Framework.Views.Events;

public class CursorMoveEvent(Surface surface, Vector2<float> position) : Event(surface)
{
    public Vector2<float> Position = position;
}