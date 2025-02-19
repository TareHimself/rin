using System.Numerics;
using rin.Framework.Graphics.Windows;
using rin.Framework.Views.Graphics;

namespace rin.Framework.Views.Events;

public class CursorUpEvent(Surface surface, CursorButton button, Vector2 position)
    : Event(surface)
{
    public CursorButton Button = button;
    public Vector2 Position = position;
}