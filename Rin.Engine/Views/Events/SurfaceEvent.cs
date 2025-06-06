using Rin.Engine.Views.Graphics;

namespace Rin.Engine.Views.Events;

public class SurfaceEvent(Surface surface) : ISurfaceEvent
{
    public Surface Surface { get; } = surface;
}