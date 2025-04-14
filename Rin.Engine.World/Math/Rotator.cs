using System.Numerics;
using JetBrains.Annotations;
using Rin.Engine.Math;

namespace Rin.Engine.World.Math;

public struct Rotator(float inYaw, float inPitch, float inRoll)
{
    public float Pitch = inPitch % 360.0f;
    public float Yaw = inYaw % 360.0f;
    public float Roll = inRoll % 360.0f;

    public Rotator(float inData) : this(inData, inData, inData)
    {
    }


    [PublicAPI]
    public Vector3 GetForward()
    {
        return ToQuaternion().GetForward();
    }

    [PublicAPI]
    public Vector3 GetRight()
    {
        return ToQuaternion().GetRight();
    }

    [PublicAPI]
    public Vector3 GetUp()
    {
        return ToQuaternion().GetUp();
    }

    public Rotator Delta(float? pitch = null, float? yaw = null, float? roll = null)
    {
        var dPitch = pitch ?? 0;
        var dYaw = yaw ?? 0;
        var dRoll = roll ?? 0;

        return new Rotator(Yaw + dYaw, Pitch + dPitch, Roll + dRoll);
    }

    public static float ClampAxis(float angle)
    {
        angle = angle % 360.0f;
        if (angle < 0f) angle += 360.0f;

        return angle;
    }

    public void Deconstruct(out float pitch, out float yaw, out float roll)
    {
        pitch = Pitch;
        yaw = Yaw;
        roll = Roll;
    }

    [Pure]
    public Quaternion ToQuaternion()
    {
        return Quaternion.Identity.AddPitch(Pitch).AddYaw(Yaw).AddLocalRoll(Roll);
    }
}