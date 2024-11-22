using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace rin.Core.Math;

public static partial class Glm
{
    [LibraryImport(Dlls.AeroxRuntimeNative, EntryPoint = "mathGlmOrthographic")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void NativeGlmOrthographic(ref Matrix4 result, float left, float right, float bottom,
        float top);


    public static Matrix4 Orthographic(float left, float right, float bottom, float top)
    {
        var result = Matrix4.Identity;
        NativeGlmOrthographic(ref result, left, right, bottom, top);
        return result;
    }
    
    [LibraryImport(Dlls.AeroxRuntimeNative, EntryPoint = "mathGlmPerspective")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void NativeGlmPerspective(ref Matrix4 result, float fov,float aspect,float near,float far);


    public static Matrix4 Perspective(float fov,float aspect,float near,float far)
    {
        var result = Matrix4.Identity;
        NativeGlmPerspective(ref result,fov,aspect,near,far);
        return result;
    }
}