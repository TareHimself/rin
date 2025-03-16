using System.Numerics;
using System.Text.Json.Nodes;

namespace Rin.Engine.Core.Math;

public static class MathExtensions
{
    /// <summary>
    ///     If <see cref="val" /> is finite returns <see cref="val" /> else <see cref="other" /> which is zero by default
    /// </summary>
    /// <param name="val"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static float FiniteOr(this float val, float other = 0.0f)
    {
        return float.IsFinite(val) ? val : other;
    }

    public static Vector2 FiniteOr(this Vector2 val, float x = 0.0f, float y = 0.0f)
    {
        return new Vector2(float.IsFinite(val.X) ? val.X : x, float.IsFinite(val.Y) ? val.Y : y);
    }

    public static Vector2 FiniteOr(this Vector2 val, Vector2 other)
    {
        return new Vector2(float.IsFinite(val.X) ? val.X : other.X, float.IsFinite(val.Y) ? val.Y : other.Y);
    }

    public static Vector2 Abs(this Vector2 self)
    {
        return new Vector2(System.Math.Abs(self.X), System.Math.Abs(self.Y));
    }

    // public static bool NearlyEquals(this Vector2 self, Vector2 other,float tolerance = 0.01f)
    // {
    //     return (self - other).Abs() < tolerance;
    // }

    public static double Distance(this Vector2 a, Vector2 b)
    {
        return System.Math.Sqrt(System.Math.Pow(b.X - a.X, 2) + System.Math.Pow(b.Y - a.Y, 2));
    }

    public static Vector4 ToNumericsVector(this Vec4<float> self)
    {
        return new Vector4(self.X, self.Y, self.Z, self.W);
    }

    public static Vector2 ToNumericsVector(this Vector2<float> self)
    {
        return new Vector2(self.X, self.Y);
    }

    public static Vector2 ToNumericsVector(this Vector2<double> self)
    {
        return new Vector2((float)self.X, (float)self.Y);
    }

    public static Vector2 ToNumericsVector(this Vector2<int> self)
    {
        return new Vector2(self.X, self.Y);
    }

    public static bool Within(this Vector2 self, Vector2 p1, Vector2 p2)
    {
        return p1.X <= self.X && self.X <= p2.X && p1.Y <= self.Y && self.Y <= p2.Y;
    }

    public static bool Within(this Vector2 self, Pair<Vector2, Vector2> bounds)
    {
        var (p1, p2) = bounds;
        return p1.X <= self.X && self.X <= p2.X && p1.Y <= self.Y && self.Y <= p2.Y;
    }

    public static float Dot(this Vector2 self, Vector2 other)
    {
        return Vector2.Dot(self, other);
    }

    public static double Length(this Vector2 self)
    {
        return System.Math.Sqrt(self.X * self.X + self.Y * self.Y);
    }

    public static double Acos(this Vector2 self, Vector2 other)
    {
        var dot = self.Dot(other);

        var mul = self.Length() * other.Length();
        // Calculate the cosine of the angle between the vectors
        var cosine = mul == 0 ? 0 : (double)dot / mul;

        // Calculate the angle in radians using arccosine
        return System.Math.Acos(cosine);
    }

    public static double Acosd(this Vector2 self, Vector2 other)
    {
        return self.Acos(other) * System.Math.PI / 180.0f;
    }

    public static float Cross(this Vector2 self, Vector2 other)
    {
        float ux = self.X, uy = self.Y, vx = other.X, vy = other.Y;
        return ux * vy - uy * vx;
    }

    public static Vector2 Clamp(this Vector2 self, Vector2 min, Vector2 max)
    {
        return Vector2.Clamp(self, min, max);
    }

    public static float DistanceTo(this Vector2 self, Vector2 other)
    {
        return Vector2.Distance(self, other);
    }


    public static JsonObject ToJson(this Vector2 self)
    {
        return new JsonObject
        {
            ["X"] = self.X,
            ["Y"] = self.Y
        };
    }

    public static Vector2 ToVector2(this JsonObject self) => new Vector2()
    {
        X = self["X"]?.GetValue<float>() ?? 0,
        Y = self["Y"]?.GetValue<float>() ?? 0,
    };
    
    public static JsonObject ToJson(this Vector3 self)
    {
        return new JsonObject
        {
            ["X"] = self.X,
            ["Y"] = self.Y,
            ["Z"] = self.Z
        };
    }
    
    public static Vector3 ToVector3(this JsonObject self) => new Vector3()
    {
        X = self["X"]?.GetValue<float>() ?? 0,
        Y = self["Y"]?.GetValue<float>() ?? 0,
        Z = self["Z"]?.GetValue<float>() ?? 0,
    };
    
    public static JsonObject ToJson(this Vector4 self)
    {
        return new JsonObject
        {
            ["X"] = self.X,
            ["Y"] = self.Y,
            ["Z"] = self.X,
            ["W"] = self.Y,
        };
    }
    
    public static Vector4 ToVector4(this JsonObject self) => new Vector4()
    {
        X = self["X"]?.GetValue<float>() ?? 0,
        Y = self["Y"]?.GetValue<float>() ?? 0,
        Z = self["Z"]?.GetValue<float>() ?? 0,
        W = self["W"]?.GetValue<float>() ?? 0,
    };
}