using rin.Core.Math;

namespace rin.Graphics.Windows.Events;

public class ResizeEvent : Event
{
    public required Vector2<uint> Size;
}