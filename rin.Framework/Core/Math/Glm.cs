﻿namespace rin.Framework.Core.Math;

public static class Glm
{
    public static Mat4 Orthographic(float left, float right, float bottom, float top)
    {
        var result = Mat4.Identity;
        NativeMethods.NativeGlmOrthographic(ref result, left, right, bottom, top);
        return result;
    }


    public static Mat4 Perspective(float fov, float aspect, float near, float far)
    {
        var result = Mat4.Identity;
        NativeMethods.NativeGlmPerspective(ref result, fov, aspect, near, far);
        return result;
    }
}