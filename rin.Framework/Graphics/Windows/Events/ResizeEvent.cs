using rin.Framework.Core.Math;

namespace rin.Framework.Graphics.Windows.Events;

public class ResizeEvent : Event
{
    public required Vec2<uint> Size;
}