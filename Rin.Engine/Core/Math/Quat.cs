using System.Numerics;

namespace Rin.Engine.Core.Math;

public struct Quat(float inX, float inY, float inZ, float inW)
    : ICloneable<Quat>,
        IMultiplyOperators<Quat, Quat, Quat>,
        IMultiplyOperators<Quat, Vector3, Vector3>
{
    public float X = inX;
    public float Y = inY;
    public float Z = inZ;
    public float W = inW;

    public static Quat Identity => new(0.0f, 0.0f, 0.0f, 1.0f);

    public Quat(float inData) : this(inData, inData, inData, inData)
    {
    }


    public static Quat FromAngle(float angle, Vector3 axis)
    {
        var q = Zero;
        Native.Math.QuatFromAngle(ref q, angle, ref axis);
        return q;
    }

    public static Quat FromAngleDeg(float angle, Vector3 axis)
    {
        return FromAngle((float)(angle * System.Math.PI / 180.0f), axis);
    }

    public Quat Clone()
    {
        return new Quat(X, Y, Z, W);
    }

    public static Quat Zero => new(0.0f);

    public Vector3 Forward => this * Constants.ForwardVector;

    public Vector3 Right => this * Constants.RightVector;

    public Vector3 Up => this * Constants.UpVector;

    public static explicit operator Quat(Rotator rotator)
    {
        return FromAngle(0, Constants.ForwardVector).ApplyPitch(rotator.Pitch).ApplyYaw(rotator.Yaw)
            .ApplyRoll(rotator.Roll);
    }

    public static Quat LookAt(Vector3 from, Vector3 to, Vector3 up)
    {
        var result = new Quat();
        Native.Math.QuatLookAt(ref result, ref from, ref to, ref up);
        return result;
    }

    public Quat ApplyPitch(float delta)
    {
        return this * FromAngleDeg(delta, Constants.RightVector);
    }

    public Quat ApplyRoll(float delta)
    {
        return this * FromAngleDeg(delta, Constants.ForwardVector);
    }

    public Quat ApplyYaw(float delta)
    {
        return this * FromAngleDeg(delta, Constants.UpVector);
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

        Native.Math.MultiplyQuatQuat(ref result, ref left, ref right);

        return result;
    }


    public static Vector3 operator *(Quat left, Vector3 right)
    {
        Vector3 result = new(0.0f);
        Native.Math.MultiplyQuatVector(ref result, ref left, ref right);
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
        Native.Math.QuatToMatrix4(ref r, ref quat);
        return r;
    }

    public void Deconstruct(out float x, out float y, out float z, out float w)
    {
        x = X;
        y = Y;
        z = Z;
        w = W;
    }
}