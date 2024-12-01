using rin.Framework.Core.Math;
using rin.Framework.Widgets.Graphics;

namespace rin.Framework.Widgets.Events;

public class ResizeEvent(Surface surface, Vector2<int> newSize) : Event(surface)
{
    public Vector2<int> Size = newSize;
}