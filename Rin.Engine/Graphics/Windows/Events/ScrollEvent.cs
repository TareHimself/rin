using System.Numerics;
using Rin.Engine.Core.Math;

namespace Rin.Engine.Graphics.Windows.Events;

public class ScrollEvent : CursorEvent
{
    public required Vector2 Delta;
}