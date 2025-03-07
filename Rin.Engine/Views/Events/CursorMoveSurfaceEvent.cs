using System.Numerics;
using Rin.Engine.Views.Graphics;

namespace Rin.Engine.Views.Events;

public class CursorMoveSurfaceEvent(Surface surface, Vector2 position,View? target = null) : CursorSurfaceEvent(surface), IHandleableEvent
{
    public Vector2 Position = position;
    
    public readonly List<View> Over = [];
    
    public bool Handled => Over.Count != 0;
}