﻿using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace rin.Framework.Core.Math;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public partial struct Matrix4 : ICloneable<Matrix4>, IMultiplyOperators<Matrix4, Matrix4, Matrix4>,
    IMultiplyOperators<Matrix4, Vector4<float>, Vector4<float>>
{
    public Vector4<float> Column1 = new(0.0f);

    public Vector4<float> Column2 = new(0.0f);

    public Vector4<float> Column3 = new(0.0f);

    public Vector4<float> Column4 = new(0.0f);

    public Matrix4()
    {
    }

    public static Matrix4 Identity => new()
    {
        Column1 = new Vector4<float>(1.0f, 0.0f, 0.0f, 0.0f),
        Column2 = new Vector4<float>(0.0f, 1.0f, 0.0f, 0.0f),
        Column3 = new Vector4<float>(0.0f, 0.0f, 1.0f, 0.0f),
        Column4 = new Vector4<float>(0.0f, 0.0f, 0.0f, 1.0f)
    };


    public Matrix4 Clone()
    {
        return new Matrix4()
        {
            Column1 = Column1.Clone(),
            Column2 = Column2.Clone(),
            Column3 = Column3.Clone(),
            Column4 = Column4.Clone()
        };
    }


    
    public Matrix4 Inverse()
    {
        var r = Clone();
        NativeMethods.NativeInverse(ref r, ref this);
        return r;
    }


    

    public Matrix4 Translate(Vector3<float> translation)
    {
        var r = Clone();
        NativeMethods.NativeTranslate(ref r, ref this, ref translation);
        return r;
    }
    

    

    public Matrix4 Scale(Vector3<float> scale)
    {
        var r = Clone();
        NativeMethods.NativeScale(ref r, ref this, ref scale);
        return r;
    }

    public Matrix4 Rotate(Quaternion rotation)
    {
        var r = Clone();

        return r * rotation;
    }

    public Matrix4 Rotate(float angle, Vector3<float> axis)
    {
        var r = Clone();
        NativeMethods.NativeRotate(ref r, ref this, angle, ref axis);
        return r;
    }

    public Matrix4 RotateDeg(float angle, Vector3<float> axis)
    {
        var r = Clone();
        NativeMethods.NativeRotate(ref r, ref this, float.DegreesToRadians(angle), ref axis);
        return r;
    }
    

    public static Matrix4 operator *(Matrix4 left, Matrix4 right)
    {
        Matrix4 result = new();
        NativeMethods.NativeMultiplyMatrix4Matrix4(ref result, ref left, ref right);
        return result;
    }

    

    public static Vector4<float> operator *(Matrix4 left, Vector4<float> right)
    {
        Vector4<float> result = new(0.0f);
        NativeMethods.NativeMultiplyMatrix4Vector4(ref result, ref left, ref right);
        return result;
    }
}