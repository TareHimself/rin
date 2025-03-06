using System.Numerics;
using Rin.Engine.Views.Graphics;

namespace Rin.Engine.Views.Events;

public class SurfaceCursorMoveEvent(Surface surface, Vector2 position) : SurfaceCursorEvent(surface), IHandleableEvent
{
    public Vector2 Position = position;
    public View? Handler { get; set; }
    
    public readonly List<View> Over = [];
}