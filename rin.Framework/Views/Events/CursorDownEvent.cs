using rin.Framework.Core.Math;
using rin.Framework.Graphics.Windows;
using rin.Framework.Views.Graphics;


namespace rin.Framework.Views.Events;

public class CursorDownEvent(Surface surface, CursorButton button, Vector2<float> position)
    : Event(surface)
{
    public Vector2<float> Position = position;
    public CursorButton Button = button;
}