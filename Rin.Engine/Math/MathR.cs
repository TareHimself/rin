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
    public static Matrix4x4 PerspectiveProjection(float fieldOfViewX, float fieldOfViewY, float near, float far)
    {
        var y = 1 / float.Tan(float.DegreesToRadians(fieldOfViewY));
        var x = 1 / float.Tan(float.DegreesToRadians(fieldOfViewX));

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
    public static Frustum ExtractWorldSpaceFrustum(in Matrix4x4 view, in Matrix4x4 projection,
        in Matrix4x4 viewProjection)
    {
        // Step 1: Extract clip-space frustum planes from the viewProjection matrix
        var m = viewProjection;

        Vector4 left = new(m.M14 + m.M11, m.M24 + m.M21, m.M34 + m.M31, m.M44 + m.M41);
        Vector4 right = new(m.M14 - m.M11, m.M24 - m.M21, m.M34 - m.M31, m.M44 - m.M41);
        Vector4 bottom = new(m.M14 + m.M12, m.M24 + m.M22, m.M34 + m.M32, m.M44 + m.M42);
        Vector4 top = new(m.M14 - m.M12, m.M24 - m.M22, m.M34 - m.M32, m.M44 - m.M42);
        Vector4 near = new(m.M14 + m.M13, m.M24 + m.M23, m.M34 + m.M33, m.M44 + m.M43);
        Vector4 far = new(m.M14 - m.M13, m.M24 - m.M23, m.M34 - m.M33, m.M44 - m.M43);

        // Step 2: Normalize the planes
        static Vector4 Normalize(Vector4 p)
        {
            var normal = new Vector3(p.X, p.Y, p.Z);
            var length = normal.Length();
            return p / length;
        }

        left = Normalize(left);
        right = Normalize(right);
        bottom = Normalize(bottom);
        top = Normalize(top);
        near = Normalize(near);
        far = Normalize(far);

        // Step 3: Transform to world space using inverse-transpose of the view matrix
        if (!Matrix4x4.Invert(viewProjection, out var invView))
            throw new InvalidOperationException("View matrix is not invertible");

        var invViewT = Matrix4x4.Transpose(invView);

        left = Vector4.Transform(left, invViewT);
        right = Vector4.Transform(right, invViewT);
        bottom = Vector4.Transform(bottom, invViewT);
        top = Vector4.Transform(top, invViewT);
        near = Vector4.Transform(near, invViewT);
        far = Vector4.Transform(far, invViewT);

        // Step 4: Assign to Frustum struct
        return new Frustum
        {
            Left = left,
            Right = right,
            Bottom = bottom,
            Top = top,
            Near = near,
            Far = far
        };
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
            Position = Interpolate(begin.Position, end.Position, alpha),
            Orientation = Interpolate(begin.Orientation, end.Orientation, alpha),
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