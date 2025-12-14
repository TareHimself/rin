using System.Numerics;
using Rin.Framework.Graphics;
using Rin.Framework.Views.Graphics;

namespace Rin.Framework.Views.Events;

public class ResizeSurfaceEvent(ISurface surface, in Vector2 newSize) : SurfaceEvent(surface)
{
    public Vector2 Size = newSize;
}