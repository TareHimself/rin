using rin.Framework.Core.Math;
using rin.Framework.Views.Graphics;

namespace rin.Framework.Views.Events;

public class ResizeEvent(Surface surface, Vec2<int> newSize) : Event(surface)
{
    public Vec2<int> Size = newSize;
}