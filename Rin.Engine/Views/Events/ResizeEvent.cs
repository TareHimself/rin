using Rin.Engine.Core.Math;
using Rin.Engine.Views.Graphics;

namespace Rin.Engine.Views.Events;

public class ResizeEvent(Surface surface, Vector2<int> newSize) : Event(surface)
{
    public Vector2<int> Size = newSize;
}