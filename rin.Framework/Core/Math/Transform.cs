using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace rin.Framework.Core.Math;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public partial struct Transform() : ICloneable<Transform>
{
    public Vec3<float> Location = new(0.0f);
    public Rotator Rotation = new (0.0f);
    public Vec3<float> Scale = new(1.0f);

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct NativeTransform
    {
        public Vec3<float> Location;
        public Quat Rotation;
        public Vec3<float> Scale;
    }
    
    public static implicit operator Mat4(Transform t)
    {//
        //return Mat4.Identity.Scale(t.Scale).Rotate((Quat)t.Rotation).Translate(t.Location);
        Mat4 r = new();
        var n = new NativeTransform()
        {
            Location = t.Location,
            Rotation = (Quat)t.Rotation,
            Scale = t.Scale
        };
        NativeMethods.NativeTransformToMatrix4(ref r, ref n);
        return r;
    }



    public static implicit operator Transform(Mat4 mat)
    {
        NativeTransform r = new();
        NativeMethods.NativeMatrix4ToTransform(ref r, ref mat);
        
        return new Transform()
        {
            Location = r.Location,
            Rotation = (Rotator)r.Rotation,
            Scale = r.Scale
        };
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