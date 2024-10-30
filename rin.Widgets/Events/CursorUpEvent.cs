using rin.Core.Math;
using rin.Widgets.Graphics;
using rin.Windows;

namespace rin.Widgets.Events;

public class CursorUpEvent : Event
{
    public Vector2<float> Position;
    public MouseButton Button;

    public CursorUpEvent(Surface surface,MouseButton button, Vector2<float> position) : base(surface)
    {
        Button = button;
        Position = position;
    }
}