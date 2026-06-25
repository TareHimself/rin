using System.Numerics;
using Rin.Core.Views.Graphics;

namespace Rin.Core.Views.Events;

public class ResizeSurfaceEvent(ISurface surface, in Vector2 newSize) : SurfaceEvent(surface)
{
    public Vector2 Size = newSize;
}