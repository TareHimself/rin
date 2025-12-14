using System.Numerics;
using Rin.Framework.Views.Graphics;

namespace Rin.Framework.Views.Events;

public class CursorMoveSurfaceEvent(ISurface surface,in Vector2 position)
    : CursorSurfaceEvent(surface), IPositionalEvent
{
    public readonly List<View> Over = [];

    public bool Handled => Target is not null;
    
    public View? Target { get; set; }
    public Vector2 Position { get; } = position;
    public bool ReverseTestOrder => false;
}