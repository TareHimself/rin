using System.Numerics;
using System.Runtime.CompilerServices;
using Rin.Engine.Graphics;

namespace Rin.Engine.Math;

/// <summary>
///     Provides functions and constants for math in Rin
/// </summary>
public static class MathR
{
    public static Vector3 Up => Vector3.UnitY;
    public static Vector3 Right => Vector3.UnitX;
    public static Vector3 Forward => Vector3.UnitZ;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 PerspectiveProjection(float fieldOfView, float width, float height, float near, float far)
    {
        var tan = 1 / float.Tan(float.DegreesToRadians(fieldOfView / 2));
        var aspect = width / height;
        var y = tan;
        var x = y / aspect;

        var mat = Matrix4x4.Identity;

        mat[0, 0] = x;
        mat[1, 1] = -y;
        mat[2, 2] = near * near * near / (far + near);
        mat[2, 3] = 1f;
        mat[3, 3] = 0f;
        mat[3, 2] = near * far / (far + near);
        
        return mat;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 PerspectiveProjection(float fieldOfView, in Extent2D extent, float near, float far)
    {
        return PerspectiveProjection(fieldOfView, extent.Width, extent.Height, near, far);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 OrthographicProjection(float left, float right, float top, float bottom, float near,
        float far)
    {
        return Matrix4x4.CreateOrthographicOffCenter(left, right, top, bottom, near, far);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 ViewportProjection(float width, float height, float near, float far)
    {
        return OrthographicProjection(0.0f, width, 0.0f, height, near, far);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Interpolate(in Vector2 begin, in Vector2 end, float alpha)
    {
        return begin + alpha * (end - begin);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Interpolate(in Vector3 begin, in Vector3 end, float alpha)
    {
        return begin + alpha * (end - begin);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 Interpolate(in Vector4 begin, in Vector4 end, float alpha)
    {
        return begin + alpha * (end - begin);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion Interpolate(in Quaternion begin, in Quaternion end, float alpha)
    {
        return Quaternion.Lerp(begin, end, alpha);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Transform Interpolate(in Transform begin, in Transform end, float alpha)
    {
        return new Transform
        {
            Location = Interpolate(begin.Location, end.Location, alpha),
            Rotation = Interpolate(begin.Rotation, end.Rotation, alpha),
            Scale = Interpolate(begin.Scale, end.Scale, alpha)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion LookTowards(in Vector3 position, in Vector3 direction, in Vector3 up)
    {
        return Quaternion.CreateFromRotationMatrix(Matrix4x4.CreateLookToLeftHanded(position, direction, up));
        // // Normalize input vectors
        // var forward = Vector3.Normalize(targetDirection);
        // var up = Vector3.Normalize(upDirection);
        //
        // // Compute the right vector using cross product of up and forward
        // var right = Vector3.Normalize(Vector3.Cross(up, forward));
        //
        // // Recompute up to ensure orthogonality (since initial up might not be perfectly orthogonal to forward)
        // up = Vector3.Cross(forward, right);
        //
        // // Create a rotation matrix from the right, up, and forward vectors
        // Matrix4x4 rotationMatrix = new Matrix4x4(
        //     right.X, right.Y, right.Z, 0,
        //     up.X, up.Y, up.Z, 0,
        //     forward.X, forward.Y, forward.Z, 0,
        //     0, 0, 0, 1);
        //
        // // Convert rotation matrix to quaternion
        // Quaternion rotation = Quaternion.CreateFromRotationMatrix(rotationMatrix);
        // return rotation;
    }
}