using Rin.Framework.Views.Graphics;

namespace Rin.Framework.Views.Events;

public class SurfaceEvent(ISurface surface) : ISurfaceEvent
{
    public ISurface Surface { get; } = surface;
}