using System.Numerics;
using Rin.Engine.Views.Graphics;

namespace Rin.Engine.Views.Events;

public class ScrollSurfaceEvent(Surface surface, Vector2 position, Vector2 delta)
    : SurfaceEvent(surface) , IHandleableEvent
{
    public Vector2 Delta = delta;
    public Vector2 Position = position;
    public View? Target { get; set; }
    public bool Handled => Target != null;
}