using rin.Core.Math;
using rin.Widgets.Graphics;
using rin.Windows;

namespace rin.Widgets.Events;

public class CursorDownEvent : Event
{
    public Vector2<float> Position;
    public MouseButton Button;

    public CursorDownEvent(Surface surface,MouseButton button, Vector2<float> position) : base(surface)
    {
        Button = button;
        Position = position;
    }
}