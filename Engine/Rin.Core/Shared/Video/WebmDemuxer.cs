using System.Runtime.InteropServices;
using Rin.Core.Graphics;

namespace Rin.Core.Shared.Video;

/// <summary>
///     The one type in video playback that calls <c>Rin.Native</c>: a video context, the pinned source behind it and
///     the audio callback native drives from the decode thread.
/// </summary>
internal sealed class WebmDemuxer : IWebmDemuxer
{
    private IntPtr _context;
    private GCHandle _audioHandlerHandle;
    private InternalSource? _source;

    public WebmDemuxer(AudioPacketHandler onAudioPacket)
    {
        _context = Native.CreateVideoContext();
        _audioHandlerHandle = GCHandle.Alloc(onAudioPacket, GCHandleType.Normal);
        unsafe
        {
            Native.videoContextSetAudioCallback(_context, &OnAudioCallback, GCHandle.ToIntPtr(_audioHandlerHandle));
        }
    }

    public Exception? SourceError => _source?.Error;

    public bool HasVideo => Native.videoContextHasVideo(_context) == 1;
    public bool HasAudio => Native.videoContextHasAudio(_context) == 1;
    public double Duration => Native.videoContextGetDuration(_context);
    public double Position => Native.videoContextGetPosition(_context);
    public Extent2D VideoExtent => Native.videoContextGetVideoExtent(_context);
    public ulong FrameByteSize => Native.videoContextGetFrameByteSize(_context);
    public int AudioSampleRate => Native.videoContextGetAudioSampleRate(_context);
    public int AudioChannels => Native.videoContextGetAudioChannels(_context);
    public int AudioTrackCount => Native.videoContextGetAudioTrackCount(_context);
    public int BufferedFrameCount => Native.videoContextGetBufferedFrameCount(_context);

    public ByteRange NeededRange
    {
        get
        {
            var range = Native.videoContextGetNeededRange(_context);
            return new ByteRange(range.Start, range.End);
        }
    }

    public void SetSource(IVideoSource source)
    {
        // The decode thread may be reading the old source until native has switched to the new one.
        var previous = _source;
        var next = new InternalSource(source);
        Native.videoContextSetSource(_context, next.NativeSource);
        _source = next;
        previous?.Dispose();
    }

    public VideoDemuxResult EnsureHeader() => (VideoDemuxResult)Native.videoContextEnsureHeader(_context);

    public VideoDemuxResult Decode(double stepSeconds) =>
        (VideoDemuxResult)Native.videoContextDecode(_context, stepSeconds);

    public VideoDemuxResult Seek(double seconds) => (VideoDemuxResult)Native.videoContextSeek(_context, seconds);

    public void DropFramesBefore(double seconds) => Native.videoContextDropFramesBefore(_context, seconds);

    public void CopyRecentFrame(double time, IntPtr destination, ulong size) =>
        Native.videoContextCopyRecentFrame(_context, time, destination, size);

    public void Dispose()
    {
        if (_context == IntPtr.Zero) return;
        Native.videoContextFree(_context);
        _context = IntPtr.Zero;
        _audioHandlerHandle.Free();
        _source?.Dispose();
        _source = null;
    }

    [UnmanagedCallersOnly]
    private static unsafe void OnAudioCallback(float* data, int count, double time, IntPtr context)
    {
        if (GCHandle.FromIntPtr(context).Target is AudioPacketHandler handler)
            handler(new ReadOnlySpan<byte>(data, count * sizeof(float)), time);
    }

    /// <summary>
    ///     Pins a managed source and hands native its callbacks. A callback must not throw into native, so an
    ///     exception is kept in <see cref="Error" /> for the decode thread to report instead.
    /// </summary>
    private sealed class InternalSource : IDisposable
    {
        public readonly IntPtr NativeSource;
        public readonly IVideoSource VideoSource;
        private GCHandle _handle;
        private bool _disposed;

        public InternalSource(IVideoSource source)
        {
            VideoSource = source;
            _handle = GCHandle.Alloc(this, GCHandleType.Normal);
            unsafe
            {
                var callbacks = new Native.VideoSourceCallbacks
                {
                    Read = &Read,
                    Available = &Available,
                    Length = &Length,
                    AvailableFrom = &AvailableFrom,
                    MakeRangeAvailable = &MakeRangeAvailable,
                    ReleaseBefore = &ReleaseBefore,
                    UserData = GCHandle.ToIntPtr(_handle)
                };
                NativeSource = Native.CreateVideoSource(&callbacks);
            }
        }

        public volatile Exception? Error;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Native.videoSourceFree(NativeSource);
            _handle.Free();
            VideoSource.Dispose();
        }

        private static InternalSource? From(IntPtr context)
        {
            return GCHandle.FromIntPtr(context).Target as InternalSource;
        }

        private static void Fail(IntPtr context, Exception exception)
        {
            if (From(context) is { } source) source.Error ??= exception;
        }

        [UnmanagedCallersOnly]
        private static unsafe void Read(long offset, ulong size, IntPtr destination, IntPtr context)
        {
            var span = new Span<byte>(destination.ToPointer(), (int)size);
            try
            {
                From(context)?.VideoSource.Read(offset, span);
            }
            catch (Exception e)
            {
                span.Clear();
                Fail(context, e);
            }
        }

        [UnmanagedCallersOnly]
        private static long Available(IntPtr context)
        {
            try
            {
                return From(context)?.VideoSource.Available ?? 0;
            }
            catch (Exception e)
            {
                Fail(context, e);
                return 0;
            }
        }

        [UnmanagedCallersOnly]
        private static long Length(IntPtr context)
        {
            try
            {
                return From(context)?.VideoSource.Length ?? 0;
            }
            catch (Exception e)
            {
                Fail(context, e);
                return 0;
            }
        }

        [UnmanagedCallersOnly]
        private static long AvailableFrom(long offset, IntPtr context)
        {
            try
            {
                return From(context)?.VideoSource.AvailableFrom(offset) ?? offset;
            }
            catch (Exception e)
            {
                Fail(context, e);
                return offset;
            }
        }

        [UnmanagedCallersOnly]
        private static void MakeRangeAvailable(long offset, ulong size, IntPtr context)
        {
            try
            {
                From(context)?.VideoSource.MakeRangeAvailable(offset, (long)size);
            }
            catch (Exception e)
            {
                Fail(context, e);
            }
        }

        [UnmanagedCallersOnly]
        private static void ReleaseBefore(long offset, IntPtr context)
        {
            try
            {
                From(context)?.VideoSource.ReleaseBefore(offset);
            }
            catch (Exception e)
            {
                Fail(context, e);
            }
        }
    }
}
