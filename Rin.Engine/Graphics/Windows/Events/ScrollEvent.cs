using System.Numerics;

namespace Rin.Engine.Graphics.Windows.Events;

public class ScrollEvent : CursorEvent
{
    public required Vector2 Delta;
}