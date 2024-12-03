using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace rin.Framework.Core.Math;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public partial struct Matrix3 : ICloneable<Matrix3>, IMultiplyOperators<Matrix3, Matrix3, Matrix3>,
    IMultiplyOperators<Matrix3, Vector3<float>, Vector3<float>>
{
    public Vector3<float> Column1 = new(0.0f);

    public Vector3<float> Column2 = new(0.0f);

    public Vector3<float> Column3 = new(0.0f);

    public Matrix3()
    {
    }

    public static Matrix3 Identity => new()
    {
        Column1 = new Vector3<float>(1.0f, 0.0f, 0.0f),
        Column2 = new Vector3<float>(0.0f, 1.0f, 0.0f),
        Column3 = new Vector3<float>(0.0f, 0.0f, 1.0f)
    };


    public Matrix3 Clone()
    {
        return new Matrix3()
        {
            Column1 = Column1.Clone(),
            Column2 = Column2.Clone(),
            Column3 = Column3.Clone()
        };
    }


   

    public Matrix3 Inverse()
    {
        var r = Clone();
        NativeMethods.NativeInverse(ref r, ref this);
        return r;
    }

    public Matrix3 Translate(Vector2<float> translation)
    {
        var r = Clone();
        NativeMethods.NativeTranslate(ref r, ref this, ref translation);
        return r;
    }

    

    public Matrix3 Scale(Vector2<float> scale)
    {
        var r = Clone();
        NativeMethods.NativeScale(ref r, ref this, ref scale);
        return r;
    }
    
    
    public Matrix3 Rotate(float angle)
    {
        var r = Clone();
        NativeMethods.NativeRotate(ref r, ref this, angle);
        return r;
    }

    public Matrix3 RotateDeg(float angle)
    {
        var r = Clone();
        NativeMethods.NativeRotate(ref r, ref this, (float)(angle * System.Math.PI / 180.0f));
        return r;
    }


    

    public static Matrix3 operator *(Matrix3 left, Matrix3 right)
    {
        Matrix3 result = new();
        NativeMethods.NativeMultiplyMatrix3Matrix3(ref result, ref left, ref right);
        return result;
    }

   

    public static Vector3<float> operator *(Matrix3 left, Vector3<float> right)
    {
        Vector3<float> result = new(0.0f);
        NativeMethods.NativeMultiplyMatrix3Vector3(ref result, ref left, ref right);
        return result;
    }
}