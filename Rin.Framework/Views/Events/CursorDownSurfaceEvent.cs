using System.Numerics;
using Rin.Framework.Graphics.Windows;
using Rin.Framework.Views.Graphics;

namespace Rin.Framework.Views.Events;

public class CursorDownSurfaceEvent(ISurface surface, CursorButton button, Vector2 position, View? target = null)
    : CursorSurfaceEvent(surface), IHandleableEvent, IPositionalEvent, ITargetedEvent
{
    public readonly CursorButton Button = button;
    public bool Handled => Target != null;
    public Vector2 Position { get; set; } = position;
    public bool ReverseTestOrder => true;
    public IView? Target { get; set; }
}