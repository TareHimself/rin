using System.Numerics;
using Rin.Framework.Views.Graphics;

namespace Rin.Framework.Views.Events;

public class ScrollSurfaceEvent(ISurface surface, Vector2 position, Vector2 delta)
    : SurfaceEvent(surface), IHandleableEvent, IPositionalEvent, ITargetedEvent
{
    public Vector2 Delta = delta;
    public IView? Target { get; set; }
    public bool Handled => Target != null;
    public Vector2 Position { get; } = position;
    public bool ReverseTestOrder => false;
}