using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace rin.Framework.Core.Math;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public partial struct Rotator(float inYaw, float inPitch, float inRoll)
    : ICloneable<Rotator>
{
    public float Pitch = inPitch % 360.0f;
    public float Yaw = inYaw % 360.0f;
    public float Roll = inRoll % 360.0f;

    public Rotator(float inData) : this(inData, inData, inData)
    {
        
    }
    
    
    [PublicAPI]
    public Vec3<float> GetForwardVector() => ((Quat)this).Forward;

    [PublicAPI]
    public Vec3<float> GetRightVector() => ((Quat)this).Right;

    [PublicAPI]
    public Vec3<float> GetUpVector() => ((Quat)this).Up;


    public static Rotator LookAt(Vec3<float> from,Vec3<float> to, Vec3<float> up)
    {
        return (Rotator)Quat.LookAt(from,to,up);
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

    public Rotator Delta(float? pitch = null,float? yaw = null, float? roll = null)
    {
        var dPitch = pitch ?? 0;
        var dYaw = yaw ?? 0;
        var dRoll = roll ?? 0;
        
        return new Rotator(Yaw + dYaw,Pitch + dPitch,Roll + dRoll);
    }
}