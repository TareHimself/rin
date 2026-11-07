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

    /// <summary>
    ///     An infinite reversed-Z perspective projection: depth is 1 at <paramref name="near" /> and falls toward 0 with
    ///     distance, so there is no far plane. <paramref name="fieldOfView" /> is vertical, in degrees.
    /// </summary>
    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 PerspectiveProjection(float fieldOfView, float width, float height, float near)
    {
        var y = 1 / float.Tan(float.DegreesToRadians(fieldOfView / 2));
        var x = y / (width / height);

        return new Matrix4x4(
            x, 0f, 0f, 0f,
            0f, -y, 0f, 0f,
            0f, 0f, 0f, 1f,
            0f, 0f, near, 0f);
    }

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 PerspectiveProjection(float fieldOfView, in Extent2D extent, float near)
    {
        return PerspectiveProjection(fieldOfView, extent.Width, extent.Height, near);
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

    /// <summary>
    ///     The world-space planes of a view made with <see cref="PerspectiveProjection(float, float, float, float)" />.
    ///     A point p is inside a plane when <c>Vector4.Dot(plane, new Vector4(p, 1)) &gt;= 0</c>. The projection has no
    ///     far plane, so <see cref="Frustum.Far" /> sits at <paramref name="far" /> along the view's forward axis.
    /// </summary>
    [Pure]
    public static Frustum ExtractWorldSpaceFrustum(in Matrix4x4 view, in Matrix4x4 viewProjection, float far)
    {
        var m = viewProjection;
        var x = new Vector4(m.M11, m.M21, m.M31, m.M41);
        var y = new Vector4(m.M12, m.M22, m.M32, m.M42);
        var z = new Vector4(m.M13, m.M23, m.M33, m.M43);
        var w = new Vector4(m.M14, m.M24, m.M34, m.M44);

        // The projection flips Y, so clip-space -Y is up.
        return new Frustum
        {
            Left = NormalizePlane(w + x),
            Right = NormalizePlane(w - x),
            Bottom = NormalizePlane(w - y),
            Top = NormalizePlane(w + y),
            Near = NormalizePlane(w - z),
            Far = NormalizePlane(new Vector4(-view.M13, -view.M23, -view.M33, far - view.M43))
        };
    }

    private static Vector4 NormalizePlane(Vector4 plane)
    {
        return plane / new Vector3(plane.X, plane.Y, plane.Z).Length();
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