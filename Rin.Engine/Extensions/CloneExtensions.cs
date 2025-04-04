﻿using System.Numerics;

namespace Rin.Engine.Extensions;

public static class CloneExtensions
{
    public static Vector2 Clone(this Vector2 self)
    {
        return new Vector2(self.X, self.Y);
    }

    public static Vector3 Clone(this Vector3 self)
    {
        return new Vector3(self.X, self.Y, self.Z);
    }

    public static Vector4 Clone(this Vector4 self)
    {
        return new Vector4(self.X, self.Y, self.Z, self.W);
    }
}