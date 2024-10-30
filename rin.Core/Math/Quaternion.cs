using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace rin.Core.Math;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public partial struct Quaternion(float inX, float inY, float inZ, float inW)
    : ICloneable<Quaternion>,
        IMultiplyOperators<Quaternion, Quaternion, Quaternion>,
        IMultiplyOperators<Quaternion, Vector3<float>, Vector3<float>>
{
    public float X = inX;
    public float Y = inY;
    public float Z = inZ;
    public float W = inW;

    public static Quaternion Identity => new(0.0f, 0.0f, 0.0f, 1.0f);

    public Quaternion(float inData) : this(inData, inData, inData, inData)
    {
    }


    [LibraryImport(Dlls.AeroxRuntimeNative, EntryPoint = "mathQuatFromAngle")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void NativeQuatFromAngle(ref Quaternion result, float angle, ref Vector3<float> axis);

    public static Quaternion FromAngle(float angle, Vector3<float> axis)
    {
        var q = Zero;

        NativeQuatFromAngle(ref q, angle, ref axis);
        return q;
    }

    public static Quaternion FromAngleDeg(float angle, Vector3<float> axis)
    {
        return FromAngle((float)(angle * System.Math.PI / 180.0f), axis);
    }

    public Quaternion Clone()
    {
        return new Quaternion(X, Y, Z, W);
    }

    public static Quaternion Zero => new(0.0f);

    public Vector3<float> Forward => this * Vector3<float>.Forward;

    public Vector3<float> Right => this * Vector3<float>.Right;

    public Vector3<float> Up => this * Vector3<float>.Up;


    public Quaternion ApplyPitch(float delta)
    {
        return this * FromAngleDeg(delta, Vector3<float>.Right);
    }

    public Quaternion ApplyRoll(float delta)
    {
        return this * FromAngleDeg(delta, Vector3<float>.Forward);
    }

    public Quaternion ApplyYaw(float delta)
    {
        return this * FromAngleDeg(delta, Vector3<float>.Up);
    }

    public Quaternion ApplyLocalPitch(float delta)
    {
        return this * FromAngleDeg(delta, Right);
    }

    public Quaternion ApplyLocalRoll(float delta)
    {
        return this * FromAngleDeg(delta, Forward);
    }

    public Quaternion ApplyLocalYaw(float delta)
    {
        return this * FromAngleDeg(delta, Up);
    }

    [LibraryImport(Dlls.AeroxRuntimeNative, EntryPoint = "mathMultiplyQuatQuat")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void NativeMultiplyQuatQuat(ref Quaternion result, ref Quaternion left,
        ref Quaternion right);

    public static Quaternion operator *(Quaternion left, Quaternion right)
    {
        var result = Zero;

        NativeMultiplyQuatQuat(ref result,ref left,ref right);

        return result;
    }

    [LibraryImport(Dlls.AeroxRuntimeNative, EntryPoint = "mathMultiplyQuatVector4")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void NativeMultiplyQuatVector(ref Vector3<float> result, ref Quaternion left,
        ref Vector3<float> right);

    public static Vector3<float> operator *(Quaternion left, Vector3<float> right)
    {
        Vector3<float> result = new(0.0f);
        NativeMultiplyQuatVector(ref result, ref left, ref right);
        // var qv = new Vector3<float>(left.X, left.Y, left.Z);
        // var uv = qv.Cross(right);
        // var uuv = qv.Cross(uv);
        //
        // return right + ((uv * left.W) + uuv) * 2.0f;
        return result;
    }

    [LibraryImport(Dlls.AeroxRuntimeNative, EntryPoint = "mathQuatToMatrix4")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void NativeQuatToMatrix4(ref Matrix4 result, ref Quaternion target);

    public static implicit operator Matrix4(Quaternion quat)
    {
        Matrix4 r = new();
        NativeQuatToMatrix4(ref r, ref quat);
        return r;

        // float qxx = quat.X * quat.X,
        //     qyy = quat.Y * quat.Y,
        //     qzz = quat.Z * quat.Z,
        //     qxz = quat.X * quat.Z,
        //     qxy = quat.X * quat.Y,
        //     qyz = quat.Y * quat.Z,
        //     qwx = quat.W * quat.X,
        //     qwy = quat.W * quat.Y,
        //     qwz = quat.W * quat.Z;
        //
        //
        // return DenseMatrix.OfColumns([
        //     [
        //         1.0f - 2.0f * (qyy + qzz),
        //         2.0f * (qxy + qwz),
        //         2.0f * (qxz - qwy),
        //         0.0f
        //     ],
        //     [
        //         2.0f * (qxy - qwz),
        //         1.0f - 2.0f * (qxx + qzz),
        //         2.0f * (qyz + qwx),
        //         0.0f
        //     ],
        //     [
        //         2.0f * (qxz + qwy),
        //         2.0f * (qyz - qwy),
        //         1.0f - 2.0f * (qxx + qyy),
        //         0.0f
        //     ],
        //     [
        //         0.0f,
        //         0.0f,
        //         0.0f,
        //         1.0f,
        //     ]
        // ]);
    }
}