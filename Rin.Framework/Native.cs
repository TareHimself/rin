using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Rin.Framework.Graphics;
using Rin.Framework.Graphics.Windows;
// ReSharper disable InconsistentNaming
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

[assembly: DisableRuntimeMarshalling]

namespace Rin.Framework;

internal static partial class Native
{
#if OS_WINDOWS
    private const string DllName = "Rin.Framework.Native";
#elif OS_LINUX
    private const string DllName = "libRin.Framework.Native";
#elif OS_FREEBSD
#elif OS_MAC
#endif


    public static partial class Memory
    {
        [LibraryImport(DllName, EntryPoint = "memoryAllocate")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial IntPtr Allocate(ulong size);

        [LibraryImport(DllName, EntryPoint = "memorySet")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void Set(IntPtr ptr, int value, ulong size);

        [LibraryImport(DllName, EntryPoint = "memoryFree")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void Free(IntPtr ptr);
    }
    
    public static partial class Sdf
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GenerateDelegate(IntPtr data, uint pixelWidth, uint pixelHeight, uint byteSize,
            double width, double height);

        [LibraryImport(DllName, EntryPoint = "sdfContextNew")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial IntPtr ContextNew();

        [LibraryImport(DllName, EntryPoint = "sdfContextFree")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void ContextFree(IntPtr context);

        [LibraryImport(DllName, EntryPoint = "sdfContextBeginContour")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void ContextBeginContour(IntPtr context);
        
        [LibraryImport(DllName, EntryPoint = "sdfContextEndContour")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void ContextEndContour(IntPtr context);
        
        [LibraryImport(DllName, EntryPoint = "sdfContextMoveTo")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void ContextMoveTo(IntPtr context, ref Vector2 to);

        [LibraryImport(DllName, EntryPoint = "sdfContextLineTo")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void ContextLineTo(IntPtr context, ref Vector2 to);

        [LibraryImport(DllName, EntryPoint = "sdfContextQuadraticBezierTo")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void ContextQuadraticBezierTo(IntPtr context, ref Vector2 control,
            ref Vector2 to);

        [LibraryImport(DllName, EntryPoint = "sdfContextCubicBezierTo")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void ContextCubicBezierTo(IntPtr context, ref Vector2 control1,
            ref Vector2 control2,
            ref Vector2 to);

        [LibraryImport(DllName, EntryPoint = "sdfContextFinish")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void ContextFinish(IntPtr context);

        [LibraryImport(DllName, EntryPoint = "sdfContextGenerateMSDF")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void ContextGenerateMsdf(IntPtr context, float angleThreshold, float pixelRange,
            [MarshalAs(UnmanagedType.FunctionPtr)] GenerateDelegate callback);

        [LibraryImport(DllName, EntryPoint = "sdfContextGenerateMTSDF")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void ContextGenerateMtsdf(IntPtr context, float angleThreshold, float pixelRange,
            [MarshalAs(UnmanagedType.FunctionPtr)] GenerateDelegate callback);
    }


    public static partial class Video
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public unsafe delegate void AudioCallbackDelegate(float* data, int count, double time); 

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate ulong SourceAvailableCallbackDelegate();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate ulong SourceLengthCallbackDelegate();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void SourceReadCallbackDelegate(ulong position, ulong size, IntPtr destination);

        [LibraryImport(DllName, EntryPoint = "videoContextCreate")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial IntPtr ContextCreate();

        [LibraryImport(DllName, EntryPoint = "videoContextHasVideo")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial int ContextHasVideo(IntPtr context);

        [LibraryImport(DllName, EntryPoint = "videoContextGetVideoExtent")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial Extent2D ContextGetVideoExtent(IntPtr context);

        [LibraryImport(DllName, EntryPoint = "videoContextSeek")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void ContextSeek(IntPtr context, double time);

        [LibraryImport(DllName, EntryPoint = "videoContextHasAudio")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial int ContextHasAudio(IntPtr context);

        [LibraryImport(DllName, EntryPoint = "videoContextSetAudioCallback")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void ContextSetAudioCallback(IntPtr context,
            [MarshalAs(UnmanagedType.FunctionPtr)] AudioCallbackDelegate callback);

        [LibraryImport(DllName, EntryPoint = "videoContextGetAudioSampleRate")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial int ContextGetAudioSampleRate(IntPtr context);

        [LibraryImport(DllName, EntryPoint = "videoContextGetAudioChannels")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial int ContextGetAudioChannels(IntPtr context);

        [LibraryImport(DllName, EntryPoint = "videoContextGetAudioTrackCount")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial int ContextGetAudioTrackCount(IntPtr context);

        [LibraryImport(DllName, EntryPoint = "videoContextSetAudioTrack")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void ContextSetAudioTrack(IntPtr context, int track);

        [LibraryImport(DllName, EntryPoint = "videoContextGetDuration")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial double ContextGetDuration(IntPtr context);

        [LibraryImport(DllName, EntryPoint = "videoContextGetPosition")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial double ContextGetPosition(IntPtr context);

        [LibraryImport(DllName, EntryPoint = "videoContextSeek")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void ContextSeek(double time);

        [LibraryImport(DllName, EntryPoint = "videoContextDecode")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void ContextDecode(IntPtr context, double delta);

        [LibraryImport(DllName, EntryPoint = "videoContextEnded")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial int ContextEnded(IntPtr context);

        [LibraryImport(DllName, EntryPoint = "videoContextCopyRecentFrame")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial IntPtr ContextCopyRecentFrame(IntPtr context, double time);

        [LibraryImport(DllName, EntryPoint = "videoContextSetSource")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void ContextSetSource(IntPtr context, IntPtr source);

        [LibraryImport(DllName, EntryPoint = "videoContextFree")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void ContextFree(IntPtr context);

        [LibraryImport(DllName, EntryPoint = "videoSourceCreate")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial IntPtr SourceCreate(
            [MarshalAs(UnmanagedType.FunctionPtr)] SourceReadCallbackDelegate readCallback,
            [MarshalAs(UnmanagedType.FunctionPtr)] SourceAvailableCallbackDelegate availableCallback,
            [MarshalAs(UnmanagedType.FunctionPtr)] SourceLengthCallbackDelegate lengthCallback);

        [LibraryImport(DllName, EntryPoint = "videoSourceFree")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void SourceFree(IntPtr source);
    }

    public static partial class Platform
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public unsafe delegate void NativePathDelegate(char* path);

        [LibraryImport(DllName, EntryPoint = "platformInit")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void Init();

        [LibraryImport(DllName, EntryPoint = "platformShutdown")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void Shutdown();

        [LibraryImport(DllName, EntryPoint = "platformSelectFile")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void SelectFile([MarshalUsing(typeof(Utf8StringMarshaller))] string title,
            [MarshalAs(UnmanagedType.Bool)] bool multiple, [MarshalUsing(typeof(Utf8StringMarshaller))] string filter,
            [MarshalAs(UnmanagedType.FunctionPtr)] NativePathDelegate pathCallback);

        [LibraryImport(DllName, EntryPoint = "platformSelectPath")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void SelectPath([MarshalUsing(typeof(Utf8StringMarshaller))] string title,
            [MarshalAs(UnmanagedType.Bool)] bool multiple,
            [MarshalAs(UnmanagedType.FunctionPtr)] NativePathDelegate pathCallback);
    }
}