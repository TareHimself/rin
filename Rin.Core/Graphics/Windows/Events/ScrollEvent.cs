using System.Numerics;

namespace Rin.Core.Graphics.Windows.Events;

public class ScrollEvent : CursorEvent
{
    public required Vector2 Delta;
}