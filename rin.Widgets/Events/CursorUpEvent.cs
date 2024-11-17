using rin.Core.Math;
using rin.Graphics.Windows;
using rin.Widgets.Graphics;
using rin.Windows;

namespace rin.Widgets.Events;

public class CursorUpEvent(Surface surface, CursorButton button, Vector2<float> position)
    : Event(surface)
{
    public Vector2<float> Position = position;
    public CursorButton Button = button;
}