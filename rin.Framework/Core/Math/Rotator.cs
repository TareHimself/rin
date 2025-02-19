using System.Numerics;
using JetBrains.Annotations;

namespace rin.Framework.Core.Math;

public struct Rotator(float inYaw, float inPitch, float inRoll)
    : ICloneable<Rotator>
{
    public float Pitch = inPitch % 360.0f;
    public float Yaw = inYaw % 360.0f;
    public float Roll = inRoll % 360.0f;

    public Rotator(float inData) : this(inData, inData, inData)
    {
    }


    [PublicAPI]
    public Vector3 GetForwardVector()
    {
        return ((Quat)this).Forward;
    }

    [PublicAPI]
    public Vector3 GetRightVector()
    {
        return ((Quat)this).Right;
    }

    [PublicAPI]
    public Vector3 GetUpVector()
    {
        return ((Quat)this).Up;
    }


    public static Rotator LookAt(Vector3 from, Vector3 to, Vector3 up)
    {
        return (Rotator)Quat.LookAt(from, to, up);
    }

    public static explicit operator Rotator(Quat quat)
    {
        var rotator = new Rotator(0.0f);
        NativeMethods.NativeQuatToRotator(ref rotator, ref quat);
        rotator.Yaw = (float)MathUtils.RadToDeg(rotator.Yaw);
        rotator.Pitch = (float)MathUtils.RadToDeg(rotator.Pitch);
        // IDK WHY THIS NEEDS TO BE DONE, COULD BE A MISTAKE 
        rotator.Roll = -(float)MathUtils.RadToDeg(rotator.Roll);
        return rotator;
    }

    public Rotator Clone()
    {
        return new Rotator(Yaw, Pitch, Roll);
    }

    public Rotator Delta(float? pitch = null, float? yaw = null, float? roll = null)
    {
        var dPitch = pitch ?? 0;
        var dYaw = yaw ?? 0;
        var dRoll = roll ?? 0;

        return new Rotator(Yaw + dYaw, Pitch + dPitch, Roll + dRoll);
    }

    public void Deconstruct(out float pitch, out float yaw, out float roll)
    {
        pitch = Pitch;
        yaw = Yaw;
        roll = Roll;
    }
}