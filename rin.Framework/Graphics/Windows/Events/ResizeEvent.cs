using rin.Framework.Core.Math;

namespace rin.Framework.Graphics.Windows.Events;

public class ResizeEvent : WindowEvent
{
    public required Vector2<uint> Size;
}