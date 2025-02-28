using System.Numerics;
using Rin.Engine.Core.Extensions;

namespace Rin.Engine.Core.Math;

public struct Transform() : ICloneable<Transform>
{
    public Vector3 Location = new(0.0f);
    public Rotator Rotation = new(0.0f);
    public Vector3 Scale = new(1.0f);


    public struct NativeTransform
    {
        public Vector3 Location;
        public Quat Rotation;
        public Vector3 Scale;
    }

    public static implicit operator Mat4(Transform t)
    {
        //
        //return Mat4.Identity.Scale(t.Scale).Rotate((Quat)t.Rotation).Translate(t.Location);
        Mat4 r = new();
        var n = new NativeTransform
        {
            Location = t.Location,
            Rotation = (Quat)t.Rotation,
            Scale = t.Scale
        };
        Native.Math.TransformToMatrix4(ref r, ref n);
        return r;
    }


    public static implicit operator Transform(Mat4 mat)
    {
        NativeTransform r = new();
        Native.Math.Matrix4ToTransform(ref r, ref mat);

        return new Transform
        {
            Location = r.Location,
            Rotation = (Rotator)r.Rotation,
            Scale = r.Scale
        };
    }

    public Transform Clone()
    {
        return new Transform
        {
            Location = Location.Clone(),
            Rotation = Rotation.Clone(),
            Scale = Scale.Clone()
        };
    }

    public void Deconstruct(out Vector3 location, out Rotator rotation, out Vector3 scale)
    {
        location = Location;
        rotation = Rotation;
        scale = Scale;
    }
}