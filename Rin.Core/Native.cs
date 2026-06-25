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
#if OS_WINDOWS
    private const string DllName = "Rin.Native";
#elif OS_LINUX
    private const string DllName = "libRin.Native";
#elif OS_FREEBSD
#elif OS_MAC
#endif

    

    

    [LibraryImport(DllName)]
    public static partial IntPtr memoryAllocate(ulong size);

    [LibraryImport(DllName)]
    public static partial void memorySet(IntPtr ptr, int value, ulong size);

    [LibraryImport(DllName)]
    public static partial void memoryFree(IntPtr ptr);

    [LibraryImport(DllName)]
    public static partial IntPtr sdfContextNew();

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
    public static unsafe partial void sdfContextGenerateMSDF(IntPtr context, float angleThreshold, float pixelRange,
        delegate* unmanaged<IntPtr, uint, uint,uint,double,double,IntPtr, void> callback,IntPtr userData);

    [LibraryImport(DllName)]
    public static unsafe partial void sdfContextGenerateMTSDF(IntPtr context, float angleThreshold, float pixelRange,
        delegate* unmanaged<IntPtr, uint, uint,uint,double,double,IntPtr, void> callback,IntPtr userData);

    [LibraryImport(DllName)]
    public static partial IntPtr videoContextCreate();

    [LibraryImport(DllName)]
    public static partial int videoContextHasVideo(IntPtr context);

    [LibraryImport(DllName)]
    public static partial Extent2D videoContextGetVideoExtent(IntPtr context);

    [LibraryImport(DllName)]
    public static partial void videoContextSeek(IntPtr context, double time);

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
    public static partial void videoContextDecode(IntPtr context, double delta);

    [LibraryImport(DllName)]
    public static partial int videoContextEnded(IntPtr context);

    [LibraryImport(DllName)]
    public static partial IntPtr videoContextCopyRecentFrame(IntPtr context, double time);

    [LibraryImport(DllName)]
    public static partial void videoContextSetSource(IntPtr context, IntPtr source);

    [LibraryImport(DllName)]
    public static partial void videoContextFree(IntPtr context);

    
    
    [LibraryImport(DllName)]
    public static unsafe partial IntPtr videoSourceCreate(delegate* unmanaged<ulong, ulong, IntPtr,IntPtr, void> readCallback,delegate* unmanaged<IntPtr, ulong> availableCallback,delegate* unmanaged<IntPtr, ulong> lengthCallback,IntPtr userData);

    [LibraryImport(DllName)]
    public static partial void videoSourceFree(IntPtr source);

    [LibraryImport(DllName)]
    public static partial void platformInit();

    [LibraryImport(DllName)]
    public static partial void platformShutdown();
    
    [LibraryImport(DllName,StringMarshalling = StringMarshalling.Utf8)]
    public static unsafe partial void platformSelectFile(string title,
        [MarshalAs(UnmanagedType.I1)] bool multiple,string filter,
        delegate* unmanaged<char*,IntPtr, void> pathCallback,IntPtr userData);

    [LibraryImport(DllName, StringMarshalling = StringMarshalling.Utf8)]
    public static unsafe partial void platformSelectPath(string title,
        [MarshalAs(UnmanagedType.I1)] bool multiple,delegate* unmanaged<char*,IntPtr, void> pathCallback,IntPtr userData); 
}
