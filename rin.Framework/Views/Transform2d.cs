
using rin.Framework.Core.Math;

namespace rin.Framework.Views;

public struct Transform2d
{
    public float Angle = 0.0f;
    public Vector2<float> Translate = 0.0f;
    public Vector2<float> Scale = 1.0f;

    public Transform2d()
    {
    }
}