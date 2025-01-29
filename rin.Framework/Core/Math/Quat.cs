using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace rin.Framework.Core.Math;


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

    public static explicit operator Quat(Rotator rotator) => Quat.FromAngle(0,Vec3<float>.Forward).ApplyPitch(rotator.Pitch).ApplyYaw(rotator.Yaw).ApplyRoll(rotator.Roll);
    
    public static Quat LookAt(Vec3<float> from,Vec3<float> to, Vec3<float> up)
    {
        var result = new Quat();
        NativeMethods.NativeQuatLookAt(ref result,ref from,ref to, ref up);
        return result;
    }
    
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
    }
    
    public void Deconstruct(out float x, out float y, out float z,out float w)
    {
        x = X;
        y = Y;
        z = Z;
        w = W;
    }
}