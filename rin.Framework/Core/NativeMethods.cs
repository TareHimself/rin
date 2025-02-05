using System.Numerics;
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
    private const string DllName = "rin.Framework.Native";
    
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
    
    [LibraryImport(DllName, EntryPoint = "mathQuatToRotator")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeQuatToRotator(ref Rotator result, ref Quat quat);
    
    [LibraryImport(DllName, EntryPoint = "mathMultiplyQuatVector4")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeMultiplyQuatVector(ref Vector3 result, ref Quat left,
        ref Vector3 right);
    
    [LibraryImport(DllName, EntryPoint = "mathQuatFromAngle")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeQuatFromAngle(ref Quat result, float angle, ref Vector3 axis);
    
    [LibraryImport(DllName, EntryPoint = "mathMultiplyQuatQuat")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeMultiplyQuatQuat(ref Quat result, ref Quat left,
        ref Quat right);
    
    [LibraryImport(DllName, EntryPoint = "mathQuatToMatrix4")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeQuatToMatrix4(ref Mat4 result, ref Quat target);
    
    [LibraryImport(DllName, EntryPoint = "mathQuatLookAt")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeQuatLookAt(ref Quat result, ref Vector3 from,ref Vector3 to,ref Vector3 up);
    
    [LibraryImport(DllName, EntryPoint = "mathGlmOrthographic")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeGlmOrthographic(ref Mat4 result, float left, float right, float bottom,
        float top);
    
    
    [LibraryImport(DllName, EntryPoint = "mathGlmPerspective")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeGlmPerspective(ref Mat4 result, float fov,float aspect,float near,float far);
    
    [LibraryImport(DllName, EntryPoint = "mathInverseMatrix3")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeInverse(ref Mat3 result, ref Mat3 target);

    [LibraryImport(DllName, EntryPoint = "mathTranslateMatrix3")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeTranslate(ref Mat3 result, ref Mat3 target, ref Vector2 translation);
    
    [LibraryImport(DllName, EntryPoint = "mathScaleMatrix3")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeScale(ref Mat3 result, ref Mat3 target, ref Vector2 scale);

    [LibraryImport(DllName, EntryPoint = "mathRotateMatrix3")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeRotate(ref Mat3 result, ref Mat3 target, float angle);
    [LibraryImport(DllName, EntryPoint = "mathMultiplyMatrix3Matrix3")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeMultiplyMatrix3Matrix3(ref Mat3 result, ref Mat3 left, ref Mat3 right);
    
    [LibraryImport(DllName, EntryPoint = "mathMultiplyMatrix3Vector3")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeMultiplyMatrix3Vector3(ref Vector3 result, ref Mat3 left,
        ref Vector3 right);
    
    [LibraryImport(DllName, EntryPoint = "mathInverseMatrix4")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeInverse(ref Mat4 result, ref Mat4 target);
    [LibraryImport(DllName, EntryPoint = "mathTranslateMatrix4")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeTranslate(ref Mat4 result, ref Mat4 target, ref Vector3 translation);
    [LibraryImport(DllName, EntryPoint = "mathScaleMatrix4")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeScale(ref Mat4 result, ref Mat4 target, ref Vector3 scale);
    
    [LibraryImport(DllName, EntryPoint = "mathRotateMatrix4")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeRotate(ref Mat4 result, ref Mat4 target, float angle,
        ref Vector3 axis);
    
    [LibraryImport(DllName, EntryPoint = "mathMultiplyMatrix4Matrix4")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeMultiplyMatrix4Matrix4(ref Mat4 result, ref Mat4 left, ref Mat4 right);
    [LibraryImport(DllName, EntryPoint = "mathMultiplyMatrix4Vector4")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeMultiplyMatrix4Vector4(ref Vector4 result, ref Mat4 left,
        ref Vector4 right);
    
    [LibraryImport(DllName, EntryPoint = "mathMatrix4ToTransform")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeMatrix4ToTransform(ref Transform.NativeTransform result, ref Mat4 target);
    
    [LibraryImport(DllName, EntryPoint = "mathTransformToMatrix4")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void NativeTransformToMatrix4(ref Mat4 result, ref Transform.NativeTransform target);
    
    
    
}