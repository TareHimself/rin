using Rin.Engine.Core.Math;
using Rin.Engine.Views.Graphics;

namespace Rin.Engine.Views.Events;

public class ResizeSurfaceEvent(Surface surface, Vector2<int> newSize) : SurfaceEvent(surface)
{
    public Vector2<int> Size = newSize;
}