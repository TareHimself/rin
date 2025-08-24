using System.Numerics;

namespace Rin.Framework.Views;

public struct Transform2d
{
    public float Angle = 0.0f;
    public Vector2 Translate;
    public Vector2 Scale = new(1.0f);

    public Transform2d()
    {
    }
}