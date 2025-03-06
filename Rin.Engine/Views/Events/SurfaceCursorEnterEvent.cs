using System.Numerics;
using Rin.Engine.Views.Graphics;

namespace Rin.Engine.Views.Events;

public class SurfaceCursorEnterEvent(Surface surface, Vector2 position) : SurfaceCursorMoveEvent(surface,position)
{
    public List<View> Entered = [];
}