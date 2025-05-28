using Rin.Engine.Graphics;
using Rin.Engine.Math;
using Rin.Engine.Views.Graphics;

namespace Rin.Engine.Views.Events;

public class ResizeSurfaceEvent(Surface surface, in Extent2D newSize) : SurfaceEvent(surface)
{
    public Extent2D Size = newSize;
}