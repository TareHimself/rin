using System.Numerics;
using Rin.Engine.Graphics.Windows;
using Rin.Engine.Views.Graphics;

namespace Rin.Engine.Views.Events;

public class SurfaceCursorDownEvent(Surface surface, CursorButton button, Vector2 position,View? target = null)
    : SurfaceCursorEvent(surface)
{
    public readonly CursorButton Button = button;
    public Vector2 Position = position;
    public View? Target = target;
}