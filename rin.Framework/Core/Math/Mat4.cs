using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace rin.Framework.Core.Math;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public partial struct Mat4 : ICloneable<Mat4>, IMultiplyOperators<Mat4, Mat4, Mat4>,
    IMultiplyOperators<Mat4, Vec4<float>, Vec4<float>>
{
    public Vec4<float> Column1 = new(0.0f);

    public Vec4<float> Column2 = new(0.0f);

    public Vec4<float> Column3 = new(0.0f);

    public Vec4<float> Column4 = new(0.0f);

    public Mat4()
    {
    }

    public static Mat4 Identity => new()
    {
        Column1 = new Vec4<float>(1.0f, 0.0f, 0.0f, 0.0f),
        Column2 = new Vec4<float>(0.0f, 1.0f, 0.0f, 0.0f),
        Column3 = new Vec4<float>(0.0f, 0.0f, 1.0f, 0.0f),
        Column4 = new Vec4<float>(0.0f, 0.0f, 0.0f, 1.0f)
    };


    public Mat4 Clone()
    {
        return new Mat4()
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
        NativeMethods.NativeInverse(ref r, ref this);
        return r;
    }


    

    public Mat4 Translate(Vec3<float> translation)
    {
        var r = Clone();
        NativeMethods.NativeTranslate(ref r, ref this, ref translation);
        return r;
    }
    

    

    public Mat4 Scale(Vec3<float> scale)
    {
        var r = Clone();
        NativeMethods.NativeScale(ref r, ref this, ref scale);
        return r;
    }

    public Mat4 Rotate(Quat rotation)
    {
        var r = Clone();

        return r * rotation;
    }

    public Mat4 Rotate(float angle, Vec3<float> axis)
    {
        var r = Clone();
        NativeMethods.NativeRotate(ref r, ref this, angle, ref axis);
        return r;
    }

    public Mat4 RotateDeg(float angle, Vec3<float> axis)
    {
        var r = Clone();
        NativeMethods.NativeRotate(ref r, ref this, float.DegreesToRadians(angle), ref axis);
        return r;
    }
    

    public static Mat4 operator *(Mat4 left, Mat4 right)
    {
        Mat4 result = new();
        NativeMethods.NativeMultiplyMatrix4Matrix4(ref result, ref left, ref right);
        return result;
    }

    

    public static Vec4<float> operator *(Mat4 left, Vec4<float> right)
    {
        Vec4<float> result = new(0.0f);
        NativeMethods.NativeMultiplyMatrix4Vector4(ref result, ref left, ref right);
        return result;
    }
}