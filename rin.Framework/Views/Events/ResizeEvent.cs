using rin.Framework.Core.Math;
using rin.Framework.Views.Graphics;

namespace rin.Framework.Views.Events;

public class ResizeEvent(Surface surface, Vector2<int> newSize) : Event(surface)
{
    public Vector2<int> Size = newSize;
}