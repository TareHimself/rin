using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace rin.Framework;

internal static partial class Native
{
    private const string DllName = "rin.Framework.Native";


    public static partial class Sdf
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GenerateDelegate(IntPtr data, uint pixelWidth, uint pixelHeight, uint byteSize,
            double width, double height);

        [LibraryImport(DllName, EntryPoint = "sdfContextNew")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static partial IntPtr ContextNew();

        [LibraryImport(DllName, EntryPoint = "sdfContextFree")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static partial void ContextFree(IntPtr context);

        [LibraryImport(DllName, EntryPoint = "sdfContextMoveTo")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static partial void ContextMoveTo(IntPtr context, ref Vector2 to);

        [LibraryImport(DllName, EntryPoint = "sdfContextLineTo")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static partial void ContextLineTo(IntPtr context, ref Vector2 to);

        [LibraryImport(DllName, EntryPoint = "sdfContextQuadraticBezierTo")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static partial void ContextQuadraticBezierTo(IntPtr context, ref Vector2 control,
            ref Vector2 to);

        [LibraryImport(DllName, EntryPoint = "sdfContextCubicBezierTo")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static partial void ContextCubicBezierTo(IntPtr context, ref Vector2 control1,
            ref Vector2 control2,
            ref Vector2 to);

        [LibraryImport(DllName, EntryPoint = "sdfContextEnd")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static partial void ContextEnd(IntPtr context);

        [LibraryImport(DllName, EntryPoint = "sdfContextGenerateMSDF")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static partial void ContextGenerateMsdf(IntPtr context, float angleThreshold, float pixelRange,
            [MarshalAs(UnmanagedType.FunctionPtr)] GenerateDelegate callback);

        [LibraryImport(DllName, EntryPoint = "sdfContextGenerateMTSDF")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static partial void ContextGenerateMtsdf(IntPtr context, float angleThreshold, float pixelRange,
            [MarshalAs(UnmanagedType.FunctionPtr)] GenerateDelegate callback);
    }
}