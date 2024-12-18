using rin.Framework.Core;
using rin.Framework.Core.Math;
using rin.Framework.Views.Composite;

namespace rin.Framework.Views;

public class Padding : ICloneable<Padding>
{
    public float Left = 0.0f;
    public float Top = 0.0f;
    public float Right = 0.0f;
    public float Bottom = 0.0f;

    public static implicit operator Vector4<float>(Padding p) =>
        new Vector4<float>(p.Left, p.Top, p.Right, p.Bottom);

    public Padding Clone() => new Padding()
    {
        Left = Left,
        Right = Right,
        Top = Top,
        Bottom = Bottom
    };

    public static implicit operator Padding(float val) => new Padding(val);

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
    
    public Padding(float horizontal,float vertical)
    {
        Left = horizontal;
        Right = horizontal;
        Top = vertical;
        Bottom = vertical;
    }
}