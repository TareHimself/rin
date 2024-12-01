using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using rin.Framework.Core.Math;
using rin.Framework.Graphics.Windows;
using TerraFX.Interop.Vulkan;
[assembly: DisableRuntimeMarshalling]

namespace rin.Framework.Core;

internal static partial class NativeMethods
{
    private const string DllName = "rin.RuntimeN";
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public unsafe delegate void NativePathDelegate(char * path);
    
    [DllImport(DllName, EntryPoint = "platformInit", CallingConvention = CallingConvention.Cdecl)]
    public static extern void NativeInit();

    [LibraryImport(DllName, EntryPoint = "platformSelectFile")]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvCdecl) })]
    public static partial void NativeSelectFile([MarshalAs(UnmanagedType.BStr)] string title, [MarshalAs(UnmanagedType.Bool)] bool multiple,[MarshalAs(UnmanagedType.BStr)] string filter,
        [MarshalAs(UnmanagedType.FunctionPtr)] NativePathDelegate pathCallback);

    [LibraryImport(DllName, EntryPoint = "platformSelectPath")]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvCdecl) })]
    public static partial void NativeSelectPath([MarshalAs(UnmanagedType.BStr)] string title, [MarshalAs(UnmanagedType.Bool)] bool multiple,
        [MarshalAs(UnmanagedType.FunctionPtr)] NativePathDelegate pathCallback);
    
    [LibraryImport(DllName, EntryPoint = "mathMultiplyQuatVector4")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeMultiplyQuatVector(ref Vector3<float> result, ref Quaternion left,
        ref Vector3<float> right);
    
    [LibraryImport(DllName, EntryPoint = "mathQuatFromAngle")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeQuatFromAngle(ref Quaternion result, float angle, ref Vector3<float> axis);
    
    [LibraryImport(DllName, EntryPoint = "mathMultiplyQuatQuat")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeMultiplyQuatQuat(ref Quaternion result, ref Quaternion left,
        ref Quaternion right);

    [LibraryImport(DllName, EntryPoint = "mathQuatToMatrix4")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeQuatToMatrix4(ref Matrix4 result, ref Quaternion target);
    
    [LibraryImport(DllName, EntryPoint = "mathGlmOrthographic")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeGlmOrthographic(ref Matrix4 result, float left, float right, float bottom,
        float top);
    
    
    [LibraryImport(DllName, EntryPoint = "mathGlmPerspective")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeGlmPerspective(ref Matrix4 result, float fov,float aspect,float near,float far);
    
    [LibraryImport(DllName, EntryPoint = "mathInverseMatrix3")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeInverse(ref Matrix3 result, ref Matrix3 target);

    [LibraryImport(DllName, EntryPoint = "mathTranslateMatrix3")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeTranslate(ref Matrix3 result, ref Matrix3 target, ref Vector2<float> translation);
    
    [LibraryImport(DllName, EntryPoint = "mathScaleMatrix3")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeScale(ref Matrix3 result, ref Matrix3 target, ref Vector2<float> scale);

    [LibraryImport(DllName, EntryPoint = "mathRotateMatrix3")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeRotate(ref Matrix3 result, ref Matrix3 target, float angle);
    [LibraryImport(DllName, EntryPoint = "mathMultiplyMatrix3Matrix3")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeMultiplyMatrix3Matrix3(ref Matrix3 result, ref Matrix3 left, ref Matrix3 right);
    
    [LibraryImport(DllName, EntryPoint = "mathMultiplyMatrix3Vector3")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeMultiplyMatrix3Vector3(ref Vector3<float> result, ref Matrix3 left,
        ref Vector3<float> right);
    
    [LibraryImport(DllName, EntryPoint = "mathInverseMatrix4")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeInverse(ref Matrix4 result, ref Matrix4 target);
    [LibraryImport(DllName, EntryPoint = "mathTranslateMatrix4")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeTranslate(ref Matrix4 result, ref Matrix4 target, ref Vector3<float> translation);
    [LibraryImport(DllName, EntryPoint = "mathScaleMatrix4")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeScale(ref Matrix4 result, ref Matrix4 target, ref Vector3<float> scale);
    
    [LibraryImport(DllName, EntryPoint = "mathRotateMatrix4")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeRotate(ref Matrix4 result, ref Matrix4 target, float angle,
        ref Vector3<float> axis);
    
    [LibraryImport(DllName, EntryPoint = "mathMultiplyMatrix4Matrix4")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeMultiplyMatrix4Matrix4(ref Matrix4 result, ref Matrix4 left, ref Matrix4 right);
    [LibraryImport(DllName, EntryPoint = "mathMultiplyMatrix4Vector4")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeMultiplyMatrix4Vector4(ref Vector4<float> result, ref Matrix4 left,
        ref Vector4<float> right);
    
    [LibraryImport(DllName, EntryPoint = "mathMatrix4ToTransform")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeMatrix4ToTransform(ref Transform result, ref Matrix4 target);
    
    [LibraryImport(DllName, EntryPoint = "mathTransformToMatrix4")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeTransformToMatrix4(ref Matrix4 result, ref Transform target);
    
    
    
}