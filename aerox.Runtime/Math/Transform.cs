using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace aerox.Runtime.Math;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public partial struct Transform() : ICloneable<Transform>
{
    public Vector3<float> Location = new(0.0f);
    public Quaternion Rotation = Quaternion.Identity;
    public Vector3<float> Scale = new(1.0f);


    public Matrix4 RelativeTo(Transform other)
    {
        return ((Matrix4)other).Inverse() * this;
    }

    public Matrix4 RelativeTo(Matrix4 other)
    {
        return other.Inverse() * this;
    }

    [LibraryImport(Dlls.AeroxRuntimeNative, EntryPoint = "mathMatrix4ToTransform")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void NativeMatrix4ToTransform(ref Matrix4 result, ref Transform target);

    public static implicit operator Matrix4(Transform t)
    {
        Matrix4 r = new();
        NativeMatrix4ToTransform(ref r, ref t);
        return r;
    }

    [LibraryImport(Dlls.AeroxRuntimeNative, EntryPoint = "mathTransformToMatrix4")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void NativeTransformToMatrix4(ref Transform result, ref Matrix4 target);

    public static implicit operator Transform(Matrix4 mat)
    {
        Transform r = new();
        NativeTransformToMatrix4(ref r, ref mat);
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