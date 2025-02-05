using System.Numerics;
using rin.Framework.Core.Math;
using rin.Framework.Views.Graphics;

namespace rin.Framework.Views.Events;

public class ScrollEvent(Surface surface, Vector2 position, Vector2 delta)
    : Event(surface)
{
    public Vector2 Delta = delta;
    public Vector2 Position = position;
}