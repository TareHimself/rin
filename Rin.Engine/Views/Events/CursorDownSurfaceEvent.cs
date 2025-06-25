using System.Numerics;
using Rin.Engine.Graphics.Windows;
using Rin.Engine.Views.Graphics;

namespace Rin.Engine.Views.Events;

public class CursorDownSurfaceEvent(Surface surface, CursorButton button, Vector2 position, View? target = null)
    : CursorSurfaceEvent(surface), IHandleableEvent, IPositionalEvent
{
    public readonly CursorButton Button = button;
    public Vector2 Position { get; set; }  = position;
    public bool ReverseTestOrder => true;
    public View? Target = target;
    public bool Handled => Target != null;
}