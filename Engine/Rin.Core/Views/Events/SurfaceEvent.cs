using Rin.Core.Views.Graphics;

namespace Rin.Core.Views.Events;

public class SurfaceEvent(ISurface surface) : ISurfaceEvent
{
    public ISurface Surface { get; } = surface;
}