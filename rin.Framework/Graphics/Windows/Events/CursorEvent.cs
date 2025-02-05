using System.Numerics;
using rin.Framework.Core.Math;

namespace rin.Framework.Graphics.Windows.Events;

public class CursorEvent : Event
{
    public required Vector2 Position;
}