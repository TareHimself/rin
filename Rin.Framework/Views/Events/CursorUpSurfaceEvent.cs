using System.Numerics;
using Rin.Framework.Graphics.Windows;
using Rin.Framework.Views.Graphics;

namespace Rin.Framework.Views.Events;

public class CursorUpSurfaceEvent(ISurface surface, CursorButton button, Vector2 position)
    : CursorSurfaceEvent(surface)
{
    public CursorButton Button = button;
    public Vector2 Position = position;
}