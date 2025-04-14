using System.Numerics;

namespace Rin.Engine.Views;

public class Padding : ICopyable<Padding>
{
    public float Bottom;
    public float Left;
    public float Right;
    public float Top;

    public Padding()
    {
    }

    public Padding(float val)
    {
        Left = val;
        Right = val;
        Top = val;
        Bottom = val;
    }

    public Padding(float horizontal, float vertical)
    {
        Left = horizontal;
        Right = horizontal;
        Top = vertical;
        Bottom = vertical;
    }

    public Padding Copy()
    {
        return new Padding
        {
            Left = Left,
            Right = Right,
            Top = Top,
            Bottom = Bottom
        };
    }

    public static implicit operator Vector4(Padding p)
    {
        return new Vector4(p.Left, p.Top, p.Right, p.Bottom);
    }

    public static implicit operator Padding(float val)
    {
        return new Padding(val);
    }
}