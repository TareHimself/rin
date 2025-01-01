using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace rin.Framework.Core.Math;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public partial struct Quat(float inX, float inY, float inZ, float inW)
    : ICloneable<Quat>,
        IMultiplyOperators<Quat, Quat, Quat>,
        IMultiplyOperators<Quat, Vec3<float>, Vec3<float>>
{
    public float X = inX;
    public float Y = inY;
    public float Z = inZ;
    public float W = inW;

    public static Quat Identity => new(0.0f, 0.0f, 0.0f, 1.0f);

    public Quat(float inData) : this(inData, inData, inData, inData)
    {
    }


    

    public static Quat FromAngle(float angle, Vec3<float> axis)
    {
        var q = Zero;

        NativeMethods.NativeQuatFromAngle(ref q, angle, ref axis);
        return q;
    }

    public static Quat FromAngleDeg(float angle, Vec3<float> axis)
    {
        return FromAngle((float)(angle * System.Math.PI / 180.0f), axis);
    }

    public Quat Clone()
    {
        return new Quat(X, Y, Z, W);
    }

    public static Quat Zero => new(0.0f);

    public Vec3<float> Forward => this * Vec3<float>.Forward;

    public Vec3<float> Right => this * Vec3<float>.Right;

    public Vec3<float> Up => this * Vec3<float>.Up;


    public Quat ApplyPitch(float delta)
    {
        return this * FromAngleDeg(delta, Vec3<float>.Right);
    }

    public Quat ApplyRoll(float delta)
    {
        return this * FromAngleDeg(delta, Vec3<float>.Forward);
    }

    public Quat ApplyYaw(float delta)
    {
        return this * FromAngleDeg(delta, Vec3<float>.Up);
    }

    public Quat ApplyLocalPitch(float delta)
    {
        return this * FromAngleDeg(delta, Right);
    }

    public Quat ApplyLocalRoll(float delta)
    {
        return this * FromAngleDeg(delta, Forward);
    }

    public Quat ApplyLocalYaw(float delta)
    {
        return this * FromAngleDeg(delta, Up);
    }

    

    public static Quat operator *(Quat left, Quat right)
    {
        var result = Zero;

        NativeMethods.NativeMultiplyQuatQuat(ref result,ref left,ref right);

        return result;
    }

    

    public static Vec3<float> operator *(Quat left, Vec3<float> right)
    {
        Vec3<float> result = new(0.0f);
        NativeMethods.NativeMultiplyQuatVector(ref result, ref left, ref right);
        // var qv = new Vector3<float>(left.X, left.Y, left.Z);
        // var uv = qv.Cross(right);
        // var uuv = qv.Cross(uv);
        //
        // return right + ((uv * left.W) + uuv) * 2.0f;
        return result;
    }

    public static implicit operator Mat4(Quat quat)
    {
        Mat4 r = new();
        NativeMethods.NativeQuatToMatrix4(ref r, ref quat);
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