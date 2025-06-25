using System.Numerics;
using Rin.Engine.Views.Graphics;

namespace Rin.Engine.Views.Events;

public class CursorMoveSurfaceEvent(Surface surface, Vector2 position, View? target = null)
    : CursorSurfaceEvent(surface), IPositionalEvent
{
    public readonly List<View> Over = [];
    public Vector2 Position { get; } = position;
    public bool ReverseTestOrder => false;

    public bool Handled { get; set; }
}