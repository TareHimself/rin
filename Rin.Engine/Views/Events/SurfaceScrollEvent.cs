using System.Numerics;
using Rin.Engine.Views.Graphics;

namespace Rin.Engine.Views.Events;

public class SurfaceScrollEvent(Surface surface, Vector2 position, Vector2 delta)
    : SurfaceEvent(surface) , IHandleableEvent
{
    public Vector2 Delta = delta;
    public Vector2 Position = position;
    public View? Handler { get; set; }
}