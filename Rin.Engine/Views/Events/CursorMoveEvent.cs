using System.Numerics;
using Rin.Engine.Views.Graphics;

namespace Rin.Engine.Views.Events;

public class CursorMoveEvent(Surface surface, Vector2 position) : Event(surface)
{
    public Vector2 Position = position;
}