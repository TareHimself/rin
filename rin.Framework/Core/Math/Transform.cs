using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace rin.Framework.Core.Math;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public partial struct Transform() : ICloneable<Transform>
{
    public Vec3<float> Location = new(0.0f);
    public Quat Rotation = Quat.Identity;
    public Vec3<float> Scale = new(1.0f);

    
    
    public static implicit operator Mat4(Transform t)
    {
        Mat4 r = new();
        NativeMethods.NativeTransformToMatrix4(ref r, ref t);
        return r;
    }



    public static implicit operator Transform(Mat4 mat)
    {
        Transform r = new();
        NativeMethods.NativeMatrix4ToTransform(ref r, ref mat);
        return r;
    }

    public Transform Clone()
    {
        return new Transform()
        {
            Location = Location.Clone(),
            Rotation = Rotation.Clone(),
            Scale = Scale.Clone()
        };
    }
}