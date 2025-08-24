using System.Numerics;
using Rin.Framework.Graphics.Windows;
using Rin.Framework.Views.Graphics;

namespace Rin.Framework.Views.Events;

public class CursorDownSurfaceEvent(Surface surface, CursorButton button, Vector2 position, View? target = null)
    : CursorSurfaceEvent(surface), IHandleableEvent, IPositionalEvent
{
    public readonly CursorButton Button = button;
    public View? Target = target;
    public bool Handled => Target != null;
    public Vector2 Position { get; set; } = position;
    public bool ReverseTestOrder => true;
}