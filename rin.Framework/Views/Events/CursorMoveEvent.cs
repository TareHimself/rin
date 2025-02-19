using System.Numerics;
using rin.Framework.Views.Graphics;

namespace rin.Framework.Views.Events;

public class CursorMoveEvent(Surface surface, Vector2 position) : Event(surface)
{
    public Vector2 Position = position;
}