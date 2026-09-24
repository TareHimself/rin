using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using Rin.Core.Graphics;

namespace Rin.Core.Shared.Math;

/// <summary>
///     Provides functions and constants for math in Rin
/// </summary>
public static class MathR
{
    public static Vector3 Up => Vector3.UnitY;
    public static Vector3 Right => Vector3.UnitX;
    public static Vector3 Forward => Vector3.UnitZ;

    [Pure]
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

    [Pure]
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

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 PerspectiveProjection(float fieldOfView, in Extent2D extent, float near, float far)
    {
        return PerspectiveProjection(fieldOfView, extent.Width, extent.Height, near, far);
    }

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 OrthographicProjection(float left, float right, float top, float bottom, float near,
        float far)
    {
        return Matrix4x4.CreateOrthographicOffCenter(left, right, top, bottom, near, far);
    }

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 ViewportProjection(float width, float height, float near, float far)
    {
        return OrthographicProjection(0.0f, width, 0.0f, height, near, far);
    }


    [Pure]
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

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Interpolate(in Vector2 begin, in Vector2 end, float alpha)
    {
        return begin + alpha * (end - begin);
    }

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Interpolate(in Vector3 begin, in Vector3 end, float alpha)
    {
        return begin + alpha * (end - begin);
    }

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 Interpolate(in Vector4 begin, in Vector4 end, float alpha)
    {
        return begin + alpha * (end - begin);
    }

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion Interpolate(in Quaternion begin, in Quaternion end, float alpha)
    {
        return Quaternion.Lerp(begin, end, alpha);
    }

    [Pure]
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

    /// <summary>
    ///     A roll-free orientation whose forward (<see cref="Forward" />) points along
    ///     <paramref name="direction" />. Built as yaw-about-world-up then local pitch — the same
    ///     composition the camera controllers use, so <c>.AddYaw()</c> / <c>.AddLocalPitch()</c>
    ///     chain onto the result consistently.
    /// </summary>
    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion LookTowards(in Vector3 direction)
    {
        if (direction.LengthSquared() < 1e-12f) return Quaternion.Identity;

        var d = Vector3.Normalize(direction);
        var pitch = -MathF.Asin(float.Clamp(d.Y, -1f, 1f));
        var yaw = MathF.Atan2(d.X, d.Z);

        return Quaternion.CreateFromAxisAngle(Up, yaw) * Quaternion.CreateFromAxisAngle(Right, pitch);
    }


    /// <summary>
    ///     If <see cref="val" /> is finite returns <see cref="val" /> else <see cref="other" /> which is zero by default
    /// </summary>
    /// <param name="val"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float FiniteOr(this in float val, in float other = 0.0f)
    {
        return float.IsFinite(val) ? val : other;
    }

    extension(in Vector2 val)
    {
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 FiniteOr(in float x = 0.0f, in float y = 0.0f)
        {
            return new Vector2(float.IsFinite(val.X) ? val.X : x, float.IsFinite(val.Y) ? val.Y : y);
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 FiniteOr(in Vector2 other)
        {
            return new Vector2(float.IsFinite(val.X) ? val.X : other.X, float.IsFinite(val.Y) ? val.Y : other.Y);
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 Abs()
        {
            return Vector2.Abs(val);
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 Floor()
        {
            return new Vector2(float.Floor(val.X), float.Floor(val.Y));
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 Ceiling()
        {
            return new Vector2(float.Ceiling(val.X), float.Ceiling(val.Y));
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Distance(in Vector2 b)
        {
            return Vector2.Distance(val, b);
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Within(in Vector2 p1, in Vector2 p2)
        {
            return p1.X <= val.X && val.X <= p2.X && p1.Y <= val.Y && val.Y <= p2.Y;
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Within(Pair<Vector2, Vector2> bounds)
        {
            var (p1, p2) = bounds;
            return p1.X <= val.X && val.X <= p2.X && p1.Y <= val.Y && val.Y <= p2.Y;
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Dot(in Vector2 other)
        {
            return Vector2.Dot(val, other);
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Length()
        {
            return float.Sqrt(val.X * val.X + val.Y * val.Y);
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Acos(in Vector2 other)
        {
            var dot = val.Dot(other);

            var mul = val.Length() * other.Length();
            // Calculate the cosine of the angle between the vectors
            var cosine = mul == 0 ? 0 : dot / mul;

            // Calculate the angle in radians using arccosine
            return float.Acos(cosine);
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Acosd(in Vector2 other)
        {
            return val.Acos(other) * float.Pi / 180.0f;
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Cross(in Vector2 other)
        {
            float ux = val.X, uy = val.Y, vx = other.X, vy = other.Y;
            return ux * vy - uy * vx;
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 Clamp(in Vector2 min, in Vector2 max)
        {
            return Vector2.Clamp(val, min, max);
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float DistanceTo(in Vector2 other)
        {
            return Vector2.Distance(val, other);
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Extent2D ToExtent()
        {
            return new Extent2D
            {
                Width = (uint)float.Ceiling(val.X),
                Height = (uint)float.Ceiling(val.Y)
            };
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 Transform(in Matrix4x4 matrix)
        {
            var vec = new Vector4(val, 0.0f, 1.0f);
            vec = Vector4.Transform(vec, matrix);
            return new Vector2(vec.X, vec.Y);
        }
    }

    extension(in Matrix4x4 matrix)
    {
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix4x4 Inverse()
        {
            Matrix4x4.Invert(matrix, out var result);
            return result;
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix4x4 Translate(in Vector2 translation)
        {
            return matrix * new Vector3(translation, 0.0f).ToTranslationMatrix();
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix4x4 Scale(in Vector2 scale)
        {
            return matrix * new Vector3(scale, 1.0f).ToScaleMatrix();
        }

        /// <summary>
        ///     Appends this operation after everything <paramref name="matrix" /> already does, i.e. applied in
        ///     <paramref name="matrix" />'s parent space, not its local space (<c>matrix * op</c>).
        /// </summary>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix4x4 Translate(in Vector3 translation)
        {
            return matrix * translation.ToTranslationMatrix();
        }

        /// <summary>
        ///     Appends this operation after everything <paramref name="matrix" /> already does, i.e. applied in
        ///     <paramref name="matrix" />'s parent space, not its local space (<c>matrix * op</c>).
        /// </summary>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix4x4 Scale(in Vector3 scale)
        {
            return matrix * scale.ToScaleMatrix();
        }

        /// <summary>
        ///     Appends this operation after everything <paramref name="matrix" /> already does, i.e. applied in
        ///     <paramref name="matrix" />'s parent space, not its local space (<c>matrix * op</c>).
        /// </summary>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix4x4 Rotate(in Quaternion rotation)
        {
            return matrix * rotation.ToRotationMatrix();
        }
    }

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 Rotate(this Matrix4x4 matrix, in float angle, in Vector3 axis)
    {
        return matrix.Rotate(Quaternion.CreateFromAxisAngle(axis, angle));
    }

    extension(in Matrix4x4 matrix)
    {
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix4x4 RotateDegrees(in float angle, in Vector3 axis)
        {
            return matrix.Rotate(Quaternion.CreateFromAxisAngle(axis, float.DegreesToRadians(angle)));
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix4x4 Rotate2D(in float angle)
        {
            if (matrix.IsIdentity) return Matrix4x4.CreateRotationZ(angle);

            return matrix * Matrix4x4.CreateRotationZ(angle);
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix4x4 Rotate2DDegrees(in float angle)
        {
            return matrix.Rotate2D(float.DegreesToRadians(angle));
        }

        /// <summary>
        ///     Places <paramref name="matrix" /> inside <paramref name="parent" />'s space: a point goes through
        ///     <paramref name="matrix" /> first, then <paramref name="parent" /> (<c>matrix * parent</c>). Use it to turn a
        ///     local transform into a world one, e.g. <c>local.ChildOf(parentWorld)</c>.
        /// </summary>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix4x4 ChildOf(in Matrix4x4 parent)
        {
            return matrix * parent;
        }

        /// <summary>
        ///     Runs <paramref name="transformation" /> before <paramref name="matrix" /> (<c>transformation * matrix</c>) -
        ///     the reverse of <see cref="ChildOf" />. Skinning is the canonical case:
        ///     <c>jointWorld.ApplyBefore(inverseBind)</c> moves a vertex into joint space, then poses it.
        /// </summary>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix4x4 ApplyBefore(in Matrix4x4 transformation)
        {
            return transformation * matrix;
        }
    }

    extension(in Vector3 src)
    {
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 Transform(in Matrix4x4 matrix)
        {
            var vec = new Vector4(src, 1.0f);
            vec = Vector4.Transform(vec, matrix);
            return new Vector3(vec.X, vec.Y, vec.Z);
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 Project(in Matrix4x4 matrix)
        {
            var vec = new Vector4(src, 1.0f);
            vec = Vector4.Transform(vec, matrix);
            vec /= vec.W;
            return new Vector3(vec.X, vec.Y, vec.Z);
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Quaternion ToQuaternion()
        {
            return LookTowards(src);
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix4x4 ToTranslationMatrix()
        {
            return Matrix4x4.CreateTranslation(src);
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix4x4 ToScaleMatrix()
        {
            return Matrix4x4.CreateScale(src);
        }
    }

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 Transform(this in Vector4 src, in Matrix4x4 matrix)
    {
        return Vector4.Transform(src, matrix);
    }

    extension(in Quaternion self)
    {
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Quaternion Add(in Vector3 axis, in float delta)
        {
            return Quaternion.CreateFromAxisAngle(axis, float.DegreesToRadians(delta)) * self;
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Quaternion AddYaw(in float delta)
        {
            return self.Add(Up, delta);
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Quaternion AddPitch(in float delta)
        {
            return self.Add(Right, delta);
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Quaternion AddRoll(in float delta)
        {
            return self.Add(Forward, delta);
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Quaternion AddLocal(in Vector3 axis, in float delta)
        {
            return self * Quaternion.CreateFromAxisAngle(axis, float.DegreesToRadians(delta));
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Quaternion AddLocalYaw(in float delta)
        {
            return self.AddLocal(Up, delta);
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Quaternion AddLocalPitch(in float delta)
        {
            return self.AddLocal(Right, delta);
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Quaternion AddLocalRoll(in float delta)
        {
            return self.AddLocal(Forward, delta);
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 GetForward()
        {
            return Vector3.Transform(Forward, self);
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 GetRight()
        {
            return Vector3.Transform(Right, self);
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 GetUp()
        {
            return Vector3.Transform(Up, self);
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix4x4 ToRotationMatrix()
        {
            return Matrix4x4.CreateFromQuaternion(self);
        }
    }

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion ToQuaternion(in this Matrix4x4 self)
    {
        return Quaternion.CreateFromRotationMatrix(self);
    }
}