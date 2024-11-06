using rin.Core.Math;
using rin.Widgets.Graphics;
using rin.Windows;

namespace rin.Widgets.Events;

public class CursorUpEvent(Surface surface, MouseButton button, Vector2<float> position)
    : Event(surface)
{
    public Vector2<float> Position = position;
    public MouseButton Button = button;
}