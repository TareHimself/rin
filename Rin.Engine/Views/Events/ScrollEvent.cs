using System.Numerics;
using Rin.Engine.Views.Graphics;

namespace Rin.Engine.Views.Events;

public class ScrollEvent(Surface surface, Vector2 position, Vector2 delta)
    : Event(surface)
{
    public Vector2 Delta = delta;
    public Vector2 Position = position;
}