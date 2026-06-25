using System.Numerics;
using Rin.Core.Graphics.Windows;
using Rin.Core.Views.Graphics;

namespace Rin.Core.Views.Events;

public class CursorUpSurfaceEvent(ISurface surface, CursorButton button, Vector2 position)
    : CursorSurfaceEvent(surface)
{
    public CursorButton Button = button;
    public Vector2 Position = position;
}