using Rin.Framework.Views.Graphics;

namespace Rin.Framework.Views.Events;

public class SurfaceEvent(Surface surface) : ISurfaceEvent
{
    public Surface Surface { get; } = surface;
}