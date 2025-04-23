using System.Numerics;
using System.Text.Json.Nodes;
using JetBrains.Annotations;
using Rin.Engine.Graphics;

namespace Rin.Engine.Math;

public static class MathExtensions
{
    /// <summary>
    ///     If <see cref="val" /> is finite returns <see cref="val" /> else <see cref="other" /> which is zero by default
    /// </summary>
    /// <param name="val"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static float FiniteOr(this in float val, in float other = 0.0f)
    {
        return float.IsFinite(val) ? val : other;
    }

    public static Vector2 FiniteOr(this in Vector2 val, in float x = 0.0f, in float y = 0.0f)
    {
        return new Vector2(float.IsFinite(val.X) ? val.X : x, float.IsFinite(val.Y) ? val.Y : y);
    }

    public static Vector2 FiniteOr(this in Vector2 val, in Vector2 other)
    {
        return new Vector2(float.IsFinite(val.X) ? val.X : other.X, float.IsFinite(val.Y) ? val.Y : other.Y);
    }

    public static Vector2 Abs(this in Vector2 self)
    {
        return new Vector2(System.Math.Abs(self.X), System.Math.Abs(self.Y));
    }

    // public static bool NearlyEquals(this Vector2 self, Vector2 other,float tolerance = 0.01f)
    // {
    //     return (self - other).Abs() < tolerance;
    // }

    public static double Distance(this in Vector2 a, in Vector2 b)
    {
        return System.Math.Sqrt(System.Math.Pow(b.X - a.X, 2) + System.Math.Pow(b.Y - a.Y, 2));
    }

    public static Vector4 ToNumericsVector(this in Vector4<float> self)
    {
        return new Vector4(self.X, self.Y, self.Z, self.W);
    }

    public static Vector2 ToNumericsVector(this in Vector2<float> self)
    {
        return new Vector2(self.X, self.Y);
    }

    public static Vector2 ToNumericsVector(this in Vector2<double> self)
    {
        return new Vector2((float)self.X, (float)self.Y);
    }

    public static Vector2 ToNumericsVector(this in Vector2<int> self)
    {
        return new Vector2(self.X, self.Y);
    }

    public static bool Within(this in Vector2 self, in Vector2 p1, in Vector2 p2)
    {
        return p1.X <= self.X && self.X <= p2.X && p1.Y <= self.Y && self.Y <= p2.Y;
    }

    public static bool Within(this in Vector2 self, Pair<Vector2, Vector2> bounds)
    {
        var (p1, p2) = bounds;
        return p1.X <= self.X && self.X <= p2.X && p1.Y <= self.Y && self.Y <= p2.Y;
    }

    public static float Dot(this in Vector2 self, in Vector2 other)
    {
        return Vector2.Dot(self, other);
    }

    public static double Length(this in Vector2 self)
    {
        return System.Math.Sqrt(self.X * self.X + self.Y * self.Y);
    }

    public static double Acos(this in Vector2 self, in Vector2 other)
    {
        var dot = self.Dot(other);

        var mul = self.Length() * other.Length();
        // Calculate the cosine of the angle between the vectors
        var cosine = mul == 0 ? 0 : (double)dot / mul;

        // Calculate the angle in radians using arccosine
        return System.Math.Acos(cosine);
    }

    public static double Acosd(this in Vector2 self, in Vector2 other)
    {
        return self.Acos(other) * System.Math.PI / 180.0f;
    }

    public static float Cross(this in Vector2 self, in Vector2 other)
    {
        float ux = self.X, uy = self.Y, vx = other.X, vy = other.Y;
        return ux * vy - uy * vx;
    }

    public static Vector2 Clamp(this in Vector2 self, in Vector2 min, in Vector2 max)
    {
        return Vector2.Clamp(self, min, max);
    }

    public static float DistanceTo(this in Vector2 self, in Vector2 other)
    {
        return Vector2.Distance(self, other);
    }


    public static JsonObject ToJson(this in Vector2 self)
    {
        return new JsonObject
        {
            ["X"] = self.X,
            ["Y"] = self.Y
        };
    }

    public static Vector2 ToVector2(this JsonObject self)
    {
        return new Vector2
        {
            X = self["X"]?.GetValue<float>() ?? 0,
            Y = self["Y"]?.GetValue<float>() ?? 0
        };
    }

    public static JsonObject ToJson(this in Vector3 self)
    {
        return new JsonObject
        {
            ["X"] = self.X,
            ["Y"] = self.Y,
            ["Z"] = self.Z
        };
    }

    public static Vector3 ToVector3(this JsonObject self)
    {
        return new Vector3
        {
            X = self["X"]?.GetValue<float>() ?? 0,
            Y = self["Y"]?.GetValue<float>() ?? 0,
            Z = self["Z"]?.GetValue<float>() ?? 0
        };
    }

    public static JsonObject ToJson(this in Vector4 self)
    {
        return new JsonObject
        {
            ["X"] = self.X,
            ["Y"] = self.Y,
            ["Z"] = self.X,
            ["W"] = self.Y
        };
    }

    public static Vector4 ToVector4(this JsonObject self)
    {
        return new Vector4
        {
            X = self["X"]?.GetValue<float>() ?? 0,
            Y = self["Y"]?.GetValue<float>() ?? 0,
            Z = self["Z"]?.GetValue<float>() ?? 0,
            W = self["W"]?.GetValue<float>() ?? 0
        };
    }
    
    public static Matrix4x4 ToTranslationMatrix(this in Vector3 self)
    {
        return Matrix4x4.CreateTranslation(self);
    }

    public static Matrix4x4 ToRotationMatrix(this in Quaternion self)
    {
        return Matrix4x4.CreateFromQuaternion(self);
    }

    public static Matrix4x4 ToScaleMatrix(this in Vector3 self)
    {
        return Matrix4x4.CreateScale(self);
    }

    public static Matrix4x4 Inverse(this in Matrix4x4 matrix)
    {
        Matrix4x4.Invert(matrix, out var result);
        return result;
    }

    public static Matrix4x4 Translate(this in Matrix4x4 matrix, in Vector2 translation)
    {
        return matrix * new Vector3(translation, 1.0f).ToTranslationMatrix();
    }


    public static Matrix4x4 Scale(this in Matrix4x4 matrix, in Vector2 scale)
    {
        return matrix * new Vector3(scale, 1.0f).ToScaleMatrix();
    }

    public static Matrix4x4 Translate(this in Matrix4x4 matrix, in Vector3 translation)
    {
        return matrix * translation.ToTranslationMatrix();
    }


    public static Matrix4x4 Scale(this in Matrix4x4 matrix, in Vector3 scale)
    {
        return matrix * scale.ToScaleMatrix();
    }

    public static Matrix4x4 Rotate(this in Matrix4x4 matrix, in Quaternion rotation)
    {
        return matrix * rotation.ToRotationMatrix();
    }

    public static Matrix4x4 Rotate(this Matrix4x4 matrix, in float angle, in Vector3 axis)
    {
        return matrix.Rotate(Quaternion.CreateFromAxisAngle(axis, angle));
    }

    public static Matrix4x4 RotateDegrees(this in Matrix4x4 matrix, in float angle, in Vector3 axis)
    {
        return matrix.Rotate(Quaternion.CreateFromAxisAngle(axis, float.DegreesToRadians(angle)));
    }

    public static Matrix4x4 Rotate2d(this in Matrix4x4 matrix, in float angle)
    {
        if (matrix.IsIdentity) return Matrix4x4.CreateRotationZ(angle);

        return matrix * Matrix4x4.CreateRotationZ(angle);
    }

    public static Matrix4x4 Rotate2dDegrees(this in Matrix4x4 matrix, in float angle)
    {
        return matrix.Rotate2d(float.DegreesToRadians(angle));
    }


    public static Vector2 Transform(this in Vector2 src, in Matrix4x4 matrix)
    {
        var vec = new Vector4(src, 0.0f, 1.0f);
        vec = Vector4.Transform(vec, matrix);
        return new Vector2(vec.X, vec.Y);
    }

    public static Vector3 Transform(this in Vector3 src, in Matrix4x4 matrix)
    {
        var vec = new Vector4(src, 1.0f);
        vec = Vector4.Transform(vec, matrix);
        return new Vector3(vec.X, vec.Y, vec.Z);
    }

    public static Vector3 Project(this in Vector3 src, in Matrix4x4 matrix)
    {
        var vec = new Vector4(src, 1.0f);
        vec = Vector4.Transform(vec, matrix);
        vec /= vec.W;
        return new Vector3(vec.X, vec.Y, vec.Z);
    }

    public static Vector4 Transform(this in Vector4 src, in Matrix4x4 matrix)
    {
        return Vector4.Transform(src, matrix);
    }

    public static Quaternion Add(this in Quaternion self, in Vector3 axis, in float delta)
    {
        return Quaternion.CreateFromAxisAngle(axis, float.DegreesToRadians(delta)) * self;
    }

    public static Quaternion AddYaw(this in Quaternion self, in float delta)
    {
        return Add(self, MathR.Up, delta);
    }

    public static Quaternion AddPitch(this in Quaternion self, in float delta)
    {
        return Add(self, MathR.Right, delta);
    }

    public static Quaternion AddRoll(this in Quaternion self, in float delta)
    {
        return Add(self, MathR.Forward, delta);
    }

    public static Quaternion AddLocal(this in Quaternion self, in Vector3 axis, in float delta)
    {
        return self * Quaternion.CreateFromAxisAngle(axis, float.DegreesToRadians(delta));
    }

    public static Quaternion AddLocalYaw(this in Quaternion self, in float delta)
    {
        return AddLocal(self, MathR.Up, delta);
    }

    public static Quaternion AddLocalPitch(this in Quaternion self, in float delta)
    {
        return AddLocal(self, MathR.Right, delta);
    }

    public static Quaternion AddLocalRoll(this in Quaternion self, in float delta)
    {
        return AddLocal(self, MathR.Forward, delta);
    }

    [Pure]
    public static Vector3 GetForward(in this Quaternion self)
    {
        return Vector3.Transform(MathR.Forward, self);
    }

    [Pure]
    public static Vector3 GetRight(in this Quaternion self)
    {
        return Vector3.Transform(MathR.Right, self);
    }

    [Pure]
    public static Vector3 GetUp(in this Quaternion self)
    {
        return Vector3.Transform(MathR.Up, self);
    }

    [Pure]
    public static Quaternion ToQuaternion(in this Matrix4x4 self)
    {
        return Quaternion.CreateFromRotationMatrix(self);
    }

    [Pure]
    public static Quaternion ToQuaternion(in this Vector3 self)
    {
        return MathR.LookTowards(Vector3.Zero, self, MathR.Up);
    }
    
    [Pure]
    public static Extent2D ToExtent(in this Vector2 self)
    {
        return new Extent2D
        {
            Width = (uint)float.Ceiling(self.X),
            Height = (uint)float.Ceiling(self.Y)
        };
    }
}