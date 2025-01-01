using rin.Framework.Core.Math;
using rin.Framework.Views.Graphics;

namespace rin.Framework.Views.Events;

public class CursorMoveEvent(Surface surface, Vec2<float> position) : Event(surface)
{
    public Vec2<float> Position = position;
}