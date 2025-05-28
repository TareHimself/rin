using Rin.Engine.Math;

namespace Rin.Engine.Graphics.Windows.Events;

public class ResizeEvent : WindowEvent
{
    public required WindowRect Rect;
    public required WindowRect DrawRect;
}