using System.Numerics;
using Rin.Engine.Graphics.Windows;
using Rin.Engine.Views.Graphics;

namespace Rin.Engine.Views.Events;

public class CursorUpSurfaceEvent(Surface surface, CursorButton button, Vector2 position)
    : CursorSurfaceEvent(surface)
{
    public CursorButton Button = button;
    public Vector2 Position = position;
}