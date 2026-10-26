using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Rin.Core.Graphics;

// ReSharper disable InconsistentNaming
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

[assembly: DisableRuntimeMarshalling]

namespace Rin.Core;

internal static partial class Native
{
    private const string DllName = "Rin.Native";

    

    

    // Mirrors CoreResult in native/Rin.Native/src/result.hpp. Keep the two in sync.
    public enum CoreResult
    {
        Ok = 0,
        InvalidArgument,
        OutOfMemory,
        PlatformError
    }

    private static void Check(CoreResult result, string function)
    {
        if (result != CoreResult.Ok) throw new NativeException(DllName, function, result);
    }

    public static IntPtr Allocate(ulong size)
    {
        var pointer = memoryAllocate(size);
        if (pointer == IntPtr.Zero && size != 0)
            throw new OutOfMemoryException($"{nameof(memoryAllocate)} of {size} bytes failed");
        return pointer;
    }

    public static IntPtr NewSdfContext()
    {
        var context = IntPtr.Zero;
        Check(sdfContextNew(ref context), nameof(sdfContextNew));
        return context;
    }

    public static unsafe void GenerateMtsdf(IntPtr context, float angleThreshold, float pixelRange,
        delegate* unmanaged<IntPtr, uint, uint, uint, double, double, IntPtr, void> callback, IntPtr userData)
    {
        Check(sdfContextGenerateMTSDF(context, angleThreshold, pixelRange, callback, userData),
            nameof(sdfContextGenerateMTSDF));
    }

    public static IntPtr CreateVideoContext()
    {
        var context = IntPtr.Zero;
        Check(videoContextCreate(ref context), nameof(videoContextCreate));
        return context;
    }

    public static unsafe IntPtr CreateVideoSource(VideoSourceCallbacks* callbacks)
    {
        var source = IntPtr.Zero;
        Check(videoSourceCreate(callbacks, ref source), nameof(videoSourceCreate));
        return source;
    }

    public static void PlatformInit()
    {
        Check(platformInit(), nameof(platformInit));
    }

    public static unsafe void SelectFile(string title, bool multiple, string filter,
        delegate* unmanaged<char*, IntPtr, void> pathCallback, IntPtr userData)
    {
        Check(platformSelectFile(title, multiple, filter, pathCallback, userData), nameof(platformSelectFile));
    }

    public static unsafe void SelectPath(string title, bool multiple,
        delegate* unmanaged<char*, IntPtr, void> pathCallback, IntPtr userData)
    {
        Check(platformSelectPath(title, multiple, pathCallback, userData), nameof(platformSelectPath));
    }

    [LibraryImport(DllName)]
    private static partial IntPtr memoryAllocate(ulong size);

    [LibraryImport(DllName)]
    public static partial void memorySet(IntPtr ptr, int value, ulong size);

    [LibraryImport(DllName)]
    public static partial void memoryFree(IntPtr ptr);

    [LibraryImport(DllName)]
    private static partial CoreResult sdfContextNew(ref IntPtr outContext);

    [LibraryImport(DllName)]
    public static partial void sdfContextFree(IntPtr context);

    [LibraryImport(DllName)]
    public static partial void sdfContextBeginContour(IntPtr context);

    [LibraryImport(DllName)]
    public static partial void sdfContextEndContour(IntPtr context);

    [LibraryImport(DllName)]
    public static partial void sdfContextMoveTo(IntPtr context, ref Vector2 to);

    [LibraryImport(DllName)]
    public static partial void sdfContextLineTo(IntPtr context, ref Vector2 to);

    [LibraryImport(DllName)]
    public static partial void sdfContextQuadraticBezierTo(IntPtr context, ref Vector2 control, ref Vector2 to);

    [LibraryImport(DllName)]
    public static partial void sdfContextCubicBezierTo(IntPtr context, ref Vector2 control1, ref Vector2 control2,
        ref Vector2 to);

    [LibraryImport(DllName)]
    public static partial void sdfContextFinish(IntPtr context);
    
    [LibraryImport(DllName)]
    private static unsafe partial CoreResult sdfContextGenerateMTSDF(IntPtr context, float angleThreshold,
        float pixelRange, delegate* unmanaged<IntPtr, uint, uint, uint, double, double, IntPtr, void> callback,
        IntPtr userData);

    [LibraryImport(DllName)]
    private static partial CoreResult videoContextCreate(ref IntPtr outContext);

    [LibraryImport(DllName)]
    public static partial int videoContextHasVideo(IntPtr context);

    [LibraryImport(DllName)]
    public static partial Extent2D videoContextGetVideoExtent(IntPtr context);

    [LibraryImport(DllName)]
    public static partial int videoContextEnsureHeader(IntPtr context);

    [LibraryImport(DllName)]
    public static partial int videoContextIsReady(IntPtr context);

    [LibraryImport(DllName)]
    public static partial int videoContextSeek(IntPtr context, double time);

    [LibraryImport(DllName)]
    public static partial int videoContextHasAudio(IntPtr context);
    
    [LibraryImport(DllName)]
    public static unsafe partial void videoContextSetAudioCallback(IntPtr context,delegate* unmanaged<float*, int, double,IntPtr, void> callback,IntPtr userData);

    [LibraryImport(DllName)]
    public static partial int videoContextGetAudioSampleRate(IntPtr context);

    [LibraryImport(DllName)]
    public static partial int videoContextGetAudioChannels(IntPtr context);

    [LibraryImport(DllName)]
    public static partial int videoContextGetAudioTrackCount(IntPtr context);

    [LibraryImport(DllName)]
    public static partial void videoContextSetAudioTrack(IntPtr context, int track);

    [LibraryImport(DllName)]
    public static partial double videoContextGetDuration(IntPtr context);

    [LibraryImport(DllName)]
    public static partial double videoContextGetPosition(IntPtr context);

    [LibraryImport(DllName)]
    public static partial int videoContextDecode(IntPtr context, double delta);

    [LibraryImport(DllName)]
    public static partial VideoByteRange videoContextGetNeededRange(IntPtr context);

    [LibraryImport(DllName)]
    public static partial int videoContextEnded(IntPtr context);

    [LibraryImport(DllName)]
    public static partial int videoContextGetBufferedFrameCount(IntPtr context);

    [LibraryImport(DllName)]
    public static partial void videoContextDropFramesBefore(IntPtr context, double time);

    [LibraryImport(DllName)]
    public static partial int videoContextCopyRecentFrame(IntPtr context, double time, IntPtr destination, ulong size);

    [LibraryImport(DllName)]
    public static partial ulong videoContextGetFrameByteSize(IntPtr context);

    [LibraryImport(DllName)]
    public static partial void videoContextSetSource(IntPtr context, IntPtr source);

    [LibraryImport(DllName)]
    public static partial void videoContextFree(IntPtr context);

    
    
    [StructLayout(LayoutKind.Sequential)]
    public struct VideoByteRange
    {
        public long Start;
        public long End;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct VideoSourceCallbacks
    {
        public delegate* unmanaged<long, ulong, IntPtr, IntPtr, void> Read;
        public delegate* unmanaged<IntPtr, long> Available;
        public delegate* unmanaged<IntPtr, long> Length;
        public delegate* unmanaged<long, IntPtr, long> AvailableFrom;
        public delegate* unmanaged<long, ulong, IntPtr, void> MakeRangeAvailable;
        public delegate* unmanaged<long, IntPtr, void> ReleaseBefore;
        public IntPtr UserData;
    }

    [LibraryImport(DllName)]
    private static unsafe partial CoreResult videoSourceCreate(VideoSourceCallbacks* callbacks, ref IntPtr outSource);

    [LibraryImport(DllName)]
    public static partial void videoSourceFree(IntPtr source);

    [LibraryImport(DllName)]
    private static partial CoreResult platformInit();

    [LibraryImport(DllName)]
    public static partial void platformShutdown();
    
    [LibraryImport(DllName, StringMarshalling = StringMarshalling.Utf8)]
    private static unsafe partial CoreResult platformSelectFile(string title,
        [MarshalAs(UnmanagedType.I1)] bool multiple, string filter,
        delegate* unmanaged<char*, IntPtr, void> pathCallback, IntPtr userData);

    [LibraryImport(DllName, StringMarshalling = StringMarshalling.Utf8)]
    private static unsafe partial CoreResult platformSelectPath(string title,
        [MarshalAs(UnmanagedType.I1)] bool multiple, delegate* unmanaged<char*, IntPtr, void> pathCallback,
        IntPtr userData);
}
