using System.Numerics;
using rin.Framework.Core.Extensions;

namespace rin.Framework.Core.Math;

public struct Mat3 : ICloneable<Mat3>, IMultiplyOperators<Mat3, Mat3, Mat3>,
    IMultiplyOperators<Mat3, Vector3, Vector3>
{
    public Vector3 Column1 = new(0.0f);

    public Vector3 Column2 = new(0.0f);

    public Vector3 Column3 = new(0.0f);

    public Mat3()
    {
    }

    public static Mat3 Identity => new()
    {
        Column1 = new Vector3(1.0f, 0.0f, 0.0f),
        Column2 = new Vector3(0.0f, 1.0f, 0.0f),
        Column3 = new Vector3(0.0f, 0.0f, 1.0f)
    };


    public Mat3 Clone()
    {
        return new Mat3
        {
            Column1 = Column1.Clone(),
            Column2 = Column2.Clone(),
            Column3 = Column3.Clone()
        };
    }


    public Mat3 Inverse()
    {
        var r = Clone();
        NativeMethods.NativeInverse(ref r, ref this);
        return r;
    }

    public Mat3 Translate(Vector2 translation)
    {
        var r = Clone();
        NativeMethods.NativeTranslate(ref r, ref this, ref translation);
        return r;
    }


    public Mat3 Scale(Vector2 scale)
    {
        var r = Clone();
        NativeMethods.NativeScale(ref r, ref this, ref scale);
        return r;
    }


    public Mat3 Rotate(float angle)
    {
        var r = Clone();
        NativeMethods.NativeRotate(ref r, ref this, angle);
        return r;
    }

    public Mat3 RotateDeg(float angle)
    {
        var r = Clone();
        NativeMethods.NativeRotate(ref r, ref this, (float)(angle * System.Math.PI / 180.0f));
        return r;
    }


    public static Mat3 operator *(Mat3 left, Mat3 right)
    {
        Mat3 result = new();
        NativeMethods.NativeMultiplyMatrix3Matrix3(ref result, ref left, ref right);
        return result;
    }


    public static Vector3 operator *(Mat3 left, Vector3 right)
    {
        Vector3 result = new(0.0f);
        NativeMethods.NativeMultiplyMatrix3Vector3(ref result, ref left, ref right);
        return result;
    }
}