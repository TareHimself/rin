using rin.Framework.Core.Math;

namespace rin.Framework.Graphics.Windows.Events;

public class CursorMoveEvent : CursorEvent
{
    public required Vec2<double> Delta;
}