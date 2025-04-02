using System.Numerics;

namespace Rin.Engine.Views.Font;

public struct GlyphRect
{
    public char Character;
    public Vector2 Position;
    public Vector2 Size;
    public float Top => Position.Y;
    public float Left => Position.X;
    public float Right => Position.X + Size.X;
    public float Bottom => Position.Y + Size.Y;
}