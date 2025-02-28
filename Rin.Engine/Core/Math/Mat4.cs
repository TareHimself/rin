using System.Numerics;
using Rin.Engine.Core.Extensions;

namespace Rin.Engine.Core.Math;

public struct Mat4 : ICloneable<Mat4>, IMultiplyOperators<Mat4, Mat4, Mat4>,
    IMultiplyOperators<Mat4, Vector4, Vector4>
{
    public Vector4 Column1;

    public Vector4 Column2;

    public Vector4 Column3;

    public Vector4 Column4;

    public Mat4()
    {
    }

    public static Mat4 Identity => new()
    {
        Column1 = new Vector4(1.0f, 0.0f, 0.0f, 0.0f),
        Column2 = new Vector4(0.0f, 1.0f, 0.0f, 0.0f),
        Column3 = new Vector4(0.0f, 0.0f, 1.0f, 0.0f),
        Column4 = new Vector4(0.0f, 0.0f, 0.0f, 1.0f)
    };


    public Mat4 Clone()
    {
        return new Mat4
        {
            Column1 = Column1.Clone(),
            Column2 = Column2.Clone(),
            Column3 = Column3.Clone(),
            Column4 = Column4.Clone()
        };
    }


    public Mat4 Inverse()
    {
        var r = Clone();
        Native.Math.Inverse(ref r, ref this);
        return r;
    }


    public Mat4 Translate(Vector3 translation)
    {
        var r = Clone();
        Native.Math.Translate(ref r, ref this, ref translation);
        return r;
    }


    public Mat4 Scale(Vector3 scale)
    {
        var r = Clone();
        Native.Math.Scale(ref r, ref this, ref scale);
        return r;
    }

    public Mat4 Rotate(Quat rotation)
    {
        var r = Clone();

        return r * rotation;
    }

    public Mat4 Rotate(float angle, Vector3 axis)
    {
        var r = Clone();
        Native.Math.Rotate(ref r, ref this, angle, ref axis);
        return r;
    }

    public Mat4 RotateDeg(float angle, Vector3 axis)
    {
        var r = Clone();
        Native.Math.Rotate(ref r, ref this, float.DegreesToRadians(angle), ref axis);
        return r;
    }


    public static Mat4 operator *(Mat4 left, Mat4 right)
    {
        Mat4 result = new();
        Native.Math.MultiplyMatrix4Matrix4(ref result, ref left, ref right);
        return result;
    }


    public static Vector4 operator *(Mat4 left, Vector4 right)
    {
        Vector4 result = new(0.0f);
        Native.Math.MultiplyMatrix4Vector4(ref result, ref left, ref right);
        return result;
    }
}