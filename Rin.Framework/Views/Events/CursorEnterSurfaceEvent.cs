using System.Numerics;
using Rin.Framework.Views.Graphics;

namespace Rin.Framework.Views.Events;

public class CursorEnterSurfaceEvent(Surface surface, Vector2 position) : CursorMoveSurfaceEvent(surface, position)
{
    public List<View> Entered = [];
}