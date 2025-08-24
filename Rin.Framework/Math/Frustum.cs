using System.Numerics;
using JetBrains.Annotations;

namespace Rin.Framework.Math;

public struct Frustum
{
    [PublicAPI] public Vector4 Left;
    [PublicAPI] public Vector4 Right;
    [PublicAPI] public Vector4 Bottom;
    [PublicAPI] public Vector4 Top;
    [PublicAPI] public Vector4 Near;
    [PublicAPI] public Vector4 Far;

    public Vector4 this[int i]
    {
        get => i switch
        {
            0 => Left,
            1 => Right,
            2 => Bottom,
            3 => Top,
            4 => Near,
            5 => Far,
            _ => throw new ArgumentOutOfRangeException(nameof(i), i, null)
        };
        set
        {
            switch (i)
            {
                case 0:
                    Left = value;
                    break;
                case 1:
                    Right = value;
                    break;
                case 2:
                    Bottom = value;
                    break;
                case 3:
                    Top = value;
                    break;
                case 4:
                    Near = value;
                    break;
                case 5:
                    Far = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(i), i, null);
            }
        }
    }
}