using System.Diagnostics.Contracts;
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

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 PerspectiveProjection(float fieldOfView, in Extent2D extent, float near, float far)
    {
        return PerspectiveProjection(fieldOfView, extent.Width, extent.Height, near, far);
    }

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 OrthographicProjection(float left, float right, float top, float bottom, float near,
        float far)
    {
        return Matrix4x4.CreateOrthographicOffCenter(left, right, top, bottom, near, far);
    }

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 ViewportProjection(float width, float height, float near, float far)
    {
        return OrthographicProjection(0.0f, width, 0.0f, height, near, far);
    }


    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Interpolate(in Vector2 begin, in Vector2 end, float alpha)
    {
        return begin + alpha * (end - begin);
    }

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Interpolate(in Vector3 begin, in Vector3 end, float alpha)
    {
        return begin + alpha * (end - begin);
    }

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 Interpolate(in Vector4 begin, in Vector4 end, float alpha)
    {
        return begin + alpha * (end - begin);
    }

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion Interpolate(in Quaternion begin, in Quaternion end, float alpha)
    {
        return Quaternion.Lerp(begin, end, alpha);
    }

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Transform Interpolate(in Transform begin, in Transform end, float alpha)
    {
        return new Transform
        {
            Position = Interpolate(begin.Position, end.Position, alpha),
            Orientation = Interpolate(begin.Orientation, end.Orientation, alpha),
            Scale = Interpolate(begin.Scale, end.Scale, alpha)
        };
    }

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    
    
    /// <summary>
    ///     If <see cref="val" /> is finite returns <see cref="val" /> else <see cref="other" /> which is zero by default
    /// </summary>
    /// <param name="val"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float FiniteOr(this in float val, in float other = 0.0f)
    {
        return float.IsFinite(val) ? val : other;
    }

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 FiniteOr(this in Vector2 val, in float x = 0.0f, in float y = 0.0f)
    {
        return new Vector2(float.IsFinite(val.X) ? val.X : x, float.IsFinite(val.Y) ? val.Y : y);
    }

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 FiniteOr(this in Vector2 val, in Vector2 other)
    {
        return new Vector2(float.IsFinite(val.X) ? val.X : other.X, float.IsFinite(val.Y) ? val.Y : other.Y);
    }

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Abs(this in Vector2 self)
    {
        
        return Vector2.Abs(self);
    }
    
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Distance(this in Vector2 a, in Vector2 b)
    {
        return Vector2.Distance(a, b);
    }
    
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Within(this in Vector2 self, in Vector2 p1, in Vector2 p2)
    {
        return p1.X <= self.X && self.X <= p2.X && p1.Y <= self.Y && self.Y <= p2.Y;
    }
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Within(this in Vector2 self, Pair<Vector2, Vector2> bounds)
    {
        var (p1, p2) = bounds;
        return p1.X <= self.X && self.X <= p2.X && p1.Y <= self.Y && self.Y <= p2.Y;
    }
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Dot(this in Vector2 self, in Vector2 other)
    {
        return Vector2.Dot(self, other);
    }
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Length(this in Vector2 self)
    {
        return float.Sqrt(self.X * self.X + self.Y * self.Y);
    }
    
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Acos(this in Vector2 self, in Vector2 other)
    {
        var dot = self.Dot(other);

        var mul = self.Length() * other.Length();
        // Calculate the cosine of the angle between the vectors
        var cosine = mul == 0 ? 0 : dot / mul;

        // Calculate the angle in radians using arccosine
        return float.Acos(cosine);
    }
    
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Acosd(this in Vector2 self, in Vector2 other)
    {
        return self.Acos(other) * float.Pi / 180.0f;
    }

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Cross(this in Vector2 self, in Vector2 other)
    {
        float ux = self.X, uy = self.Y, vx = other.X, vy = other.Y;
        return ux * vy - uy * vx;
    }

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Clamp(this in Vector2 self, in Vector2 min, in Vector2 max)
    {
        return Vector2.Clamp(self, min, max);
    }

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float DistanceTo(this in Vector2 self, in Vector2 other)
    {
        return Vector2.Distance(self, other);
    }
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 ToTranslationMatrix(this in Vector3 self)
    {
        return Matrix4x4.CreateTranslation(self);
    }
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 ToRotationMatrix(this in Quaternion self)
    {
        return Matrix4x4.CreateFromQuaternion(self);
    }
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 ToScaleMatrix(this in Vector3 self)
    {
        return Matrix4x4.CreateScale(self);
    }
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 Inverse(this in Matrix4x4 matrix)
    {
        Matrix4x4.Invert(matrix, out var result);
        return result;
    }
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 Translate(this in Matrix4x4 matrix, in Vector2 translation)
    {
        return matrix * new Vector3(translation, 1.0f).ToTranslationMatrix();
    }

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 Scale(this in Matrix4x4 matrix, in Vector2 scale)
    {
        return matrix * new Vector3(scale, 1.0f).ToScaleMatrix();
    }
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 Translate(this in Matrix4x4 matrix, in Vector3 translation)
    {
        return matrix * translation.ToTranslationMatrix();
    }
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 Scale(this in Matrix4x4 matrix, in Vector3 scale)
    {
        return matrix * scale.ToScaleMatrix();
    }
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 Rotate(this in Matrix4x4 matrix, in Quaternion rotation)
    {
        return matrix * rotation.ToRotationMatrix();
    }
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 Rotate(this Matrix4x4 matrix, in float angle, in Vector3 axis)
    {
        return matrix.Rotate(Quaternion.CreateFromAxisAngle(axis, angle));
    }
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 RotateDegrees(this in Matrix4x4 matrix, in float angle, in Vector3 axis)
    {
        return matrix.Rotate(Quaternion.CreateFromAxisAngle(axis, float.DegreesToRadians(angle)));
    }
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 Rotate2d(this in Matrix4x4 matrix, in float angle)
    {
        if (matrix.IsIdentity) return Matrix4x4.CreateRotationZ(angle);

        return matrix * Matrix4x4.CreateRotationZ(angle);
    }
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 Rotate2dDegrees(this in Matrix4x4 matrix, in float angle)
    {
        return matrix.Rotate2d(float.DegreesToRadians(angle));
    }
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Transform(this in Vector2 src, in Matrix4x4 matrix)
    {
        var vec = new Vector4(src, 0.0f, 1.0f);
        vec = Vector4.Transform(vec, matrix);
        return new Vector2(vec.X, vec.Y);
    }
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Transform(this in Vector3 src, in Matrix4x4 matrix)
    {
        var vec = new Vector4(src, 1.0f);
        vec = Vector4.Transform(vec, matrix);
        return new Vector3(vec.X, vec.Y, vec.Z);
    }
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Project(this in Vector3 src, in Matrix4x4 matrix)
    {
        var vec = new Vector4(src, 1.0f);
        vec = Vector4.Transform(vec, matrix);
        vec /= vec.W;
        return new Vector3(vec.X, vec.Y, vec.Z);
    }
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 Transform(this in Vector4 src, in Matrix4x4 matrix)
    {
        return Vector4.Transform(src, matrix);
    }
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion Add(this in Quaternion self, in Vector3 axis, in float delta)
    {
        return Quaternion.CreateFromAxisAngle(axis, float.DegreesToRadians(delta)) * self;
    }
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion AddYaw(this in Quaternion self, in float delta)
    {
        return Add(self, MathR.Up, delta);
    }
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion AddPitch(this in Quaternion self, in float delta)
    {
        return Add(self, MathR.Right, delta);
    }
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion AddRoll(this in Quaternion self, in float delta)
    {
        return Add(self, MathR.Forward, delta);
    }
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion AddLocal(this in Quaternion self, in Vector3 axis, in float delta)
    {
        return self * Quaternion.CreateFromAxisAngle(axis, float.DegreesToRadians(delta));
    }
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion AddLocalYaw(this in Quaternion self, in float delta)
    {
        return AddLocal(self, MathR.Up, delta);
    }
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion AddLocalPitch(this in Quaternion self, in float delta)
    {
        return AddLocal(self, MathR.Right, delta);
    }
    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion AddLocalRoll(this in Quaternion self, in float delta)
    {
        return AddLocal(self, MathR.Forward, delta);
    }

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 GetForward(in this Quaternion self)
    {
        return Vector3.Transform(MathR.Forward, self);
    }

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 GetRight(in this Quaternion self)
    {
        return Vector3.Transform(MathR.Right, self);
    }

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 GetUp(in this Quaternion self)
    {
        return Vector3.Transform(MathR.Up, self);
    }

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion ToQuaternion(in this Matrix4x4 self)
    {
        return Quaternion.CreateFromRotationMatrix(self);
    }

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion ToQuaternion(in this Vector3 self)
    {
        return MathR.LookTowards(Vector3.Zero, self, MathR.Up);
    }

    [Pure,MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Extent2D ToExtent(in this Vector2 self)
    {
        return new Extent2D
        {
            Width = (uint)float.Ceiling(self.X),
            Height = (uint)float.Ceiling(self.Y)
        };
    }
}