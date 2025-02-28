using System.Numerics;

namespace Rin.Engine.Core.Math;

public static class Constants
{
    public static Vector3 UpVector => new(0, 1, 0);
    public static Vector3 RightVector => new(1, 0, 0);
    public static Vector3 ForwardVector => new(0, 0, -1);
}