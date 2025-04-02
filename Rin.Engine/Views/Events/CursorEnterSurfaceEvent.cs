using System.Numerics;
using Rin.Engine.Views.Graphics;

namespace Rin.Engine.Views.Events;

public class CursorEnterSurfaceEvent(Surface surface, Vector2 position) : CursorMoveSurfaceEvent(surface, position)
{
    public List<View> Entered = [];
}