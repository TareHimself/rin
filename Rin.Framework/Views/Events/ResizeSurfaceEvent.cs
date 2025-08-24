using Rin.Framework.Graphics;
using Rin.Framework.Views.Graphics;

namespace Rin.Framework.Views.Events;

public class ResizeSurfaceEvent(Surface surface, in Extent2D newSize) : SurfaceEvent(surface)
{
    public Extent2D Size = newSize;
}