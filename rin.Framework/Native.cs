using System.Runtime.InteropServices;
using rin.Framework.Core.Math;

namespace rin.Framework;

internal static partial class Native
{
    private const string DllName = "rin.Framework.Native";


    public static partial class Sdf
    {
        [LibraryImport(DllName, EntryPoint = "sdfContextNew")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial IntPtr ContextNew();

        [LibraryImport(DllName, EntryPoint = "sdfContextFree")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial void ContextFree(IntPtr context);

        [LibraryImport(DllName, EntryPoint = "sdfContextMoveTo")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial void ContextMoveTo(IntPtr context, ref Vec2<float> to);

        [LibraryImport(DllName, EntryPoint = "sdfContextLineTo")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial void ContextLineTo(IntPtr context, ref Vec2<float> to);

        [LibraryImport(DllName, EntryPoint = "sdfContextQuadraticBezierTo")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial void ContextQuadraticBezierTo(IntPtr context, ref Vec2<float> control,
            ref Vec2<float> to);

        [LibraryImport(DllName, EntryPoint = "sdfContextCubicBezierTo")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial void ContextCubicBezierTo(IntPtr context, ref Vec2<float> control1,
            ref Vec2<float> control2,
            ref Vec2<float> to);

        [LibraryImport(DllName, EntryPoint = "sdfContextEnd")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial void ContextEnd(IntPtr context);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GenerateDelegate(IntPtr data, uint pixelWidth, uint pixelHeight, uint byteSize,
            double width, double height);

        [LibraryImport(DllName, EntryPoint = "sdfContextGenerateMSDF")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial void ContextGenerateMsdf(IntPtr context, float angleThreshold, float pixelRange,
            [MarshalAs(UnmanagedType.FunctionPtr)] GenerateDelegate callback);

        [LibraryImport(DllName, EntryPoint = "sdfContextGenerateMTSDF")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static partial void ContextGenerateMtsdf(IntPtr context, float angleThreshold, float pixelRange,
            [MarshalAs(UnmanagedType.FunctionPtr)] GenerateDelegate callback);
    }
}