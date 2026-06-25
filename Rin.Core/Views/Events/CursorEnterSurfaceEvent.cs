using System.Numerics;
using Rin.Core.Views.Graphics;

namespace Rin.Core.Views.Events;

public class CursorEnterSurfaceEvent(ISurface surface, Vector2 position) : CursorMoveSurfaceEvent(surface, position)
{
    public List<View> Entered = [];
}