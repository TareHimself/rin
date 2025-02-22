using System.Numerics;
using rin.Framework.Core.Math;

namespace rin.Framework.Graphics.Windows.Events;

public class ScrollEvent : CursorEvent
{
    public required Vector2 Delta;
}