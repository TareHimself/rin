using System.Diagnostics;
using System.Runtime.InteropServices;
using Rin.Core.Audio;
using Rin.Core.Graphics;
using Rin.Core.Shared.Buffers;
using Rin.Core.Shared.Time;

namespace Rin.Core.Shared.Video;

/// <summary>
///     Decodes webm video on another thread. The source may still be arriving: header reads, seeks and decodes that
///     run out of data are retried on every <see cref="TryDecode" />, so a view that calls it each frame keeps the
///     player going as bytes come in.
/// </summary>
public class WebmVideoPlayer : IVideoPlayer
{
    // Decoded frames wait as I420, 1.5 bytes per pixel, so this bounds the video held ahead of playback.
    private const double DecodeBufferBudgetBytes = 256.0 * 1024 * 1024;
    private const int MinBufferedFrames = 8;
    private const double DecodeAheadSeconds = 1.0;
    private const double DecodeStepSeconds = 0.1;

    private readonly IAudioModule _audioModule;
    private readonly Func<int, int, IPushStream>? _createStream;
    private readonly AutoResetEvent _decodeEvent = new(false);
    // The audio stream is made on the decode thread and read on the main thread, and a seek replaces it.
    private readonly Lock _audioLock = new();
    private readonly Task _decodeTask;

    private readonly IChronometer _videoPlaybackTime = new Chronometer();
    private IntPtr _context;
    private GCHandle _audioCallbackHandlerGcHandle;
    private double _audioPacketsStartAt;
    private IPushStream? _audioStream;

    private ulong _bufferSize;
    private int _maxBufferedFrames = MinBufferedFrames;

    // The frame on screen, which native overwrites in place when a newer one is due.
    private Buffer<byte>? _lastFrame;

    // A seek the decode thread still has to make, kept until the decoder has the data to land it.
    private SeekRequest? _pendingSeek;
    private volatile bool _ready;
    private volatile Exception? _error;

    private InternalSource? _source;
    private volatile bool _stopDecode;

    public WebmVideoPlayer(IAudioModule? audioModule = null)
    {
        _audioModule = audioModule ?? IAudioModule.Get();
        _context = Native.videoContextCreate();
        unsafe
        {
            var handler = new AudioCallbackHandler(AudioCallback);
            _audioCallbackHandlerGcHandle = GCHandle.Alloc(handler, GCHandleType.Normal);
            Native.videoContextSetAudioCallback(_context, &OnAudioCallback, GCHandle.ToIntPtr(_audioCallbackHandlerGcHandle));
        }

        _decodeTask = Task.Factory.StartNew(DecodeLoop, TaskCreationOptions.LongRunning);
    }

    public WebmVideoPlayer(Func<int, int, IPushStream> createAudioStream, IAudioModule? audioModule = null) : this(audioModule)
    {
        _createStream = createAudioStream;
    }

    public bool IsReady => _ready;
    public bool HasFailed => _error is not null;
    public Exception? Error => _error;

    public double DecodedPosition => Native.videoContextGetPosition(_context);

    public double Position
    {
        get
        {
            if (Volatile.Read(ref _pendingSeek) is { } seek) return seek.Position;
            lock (_audioLock)
            {
                return _audioStream is { } audio ? audio.Position + _audioPacketsStartAt : _videoPlaybackTime.TotalSeconds;
            }
        }
    }

    public double Duration { get; private set; }

    public ByteRange NeededRange
    {
        get
        {
            var range = Native.videoContextGetNeededRange(_context);
            return new ByteRange(range.Start, range.End);
        }
    }

    public bool IsPlaying { get; private set; }
    // Gated on _ready so a caller never sees a track before the frame buffer size that goes with it.
    public bool HasVideo => _ready && Native.videoContextHasVideo(_context) == 1;
    public int VideoTracksCount => HasVideo ? 1 : 0;
    public int SelectedVideoTrackIndex => 0;
    public Extent2D VideoExtent => Native.videoContextGetVideoExtent(_context);
    public bool HasAudio => _ready && Native.videoContextHasAudio(_context) == 1;

    public int AudioTracksCount => Native.videoContextGetAudioTrackCount(_context);
    public int SelectedAudioTrackIndex => 0;
    public int AudioSampleRate { get; private set; }
    public int AudioChannels { get; private set; }

    public void TryDecode()
    {
        if (_source == null) return;
        _decodeEvent.Set();
    }

    public void Play()
    {
        if (_source == null) return;
        IsPlaying = true;
        lock (_audioLock)
        {
            _videoPlaybackTime.Start();
            _audioStream?.Play();
        }
    }

    public void Pause()
    {
        if (_source == null) return;
        IsPlaying = false;
        lock (_audioLock)
        {
            _videoPlaybackTime.Stop();
            _audioStream?.Pause();
        }
    }

    public void Seek(double position)
    {
        if (_source == null) return;
        _lastFrame?.Dispose();
        _lastFrame = null;
        lock (_audioLock)
        {
            _videoPlaybackTime.SetTime(position);
            Volatile.Write(ref _pendingSeek, new SeekRequest(position));
            DropAudioStream();
        }

        _decodeEvent.Set();
    }

    public Buffer<byte> CopyRecentFrame()
    {
        Debug.Assert(HasVideo);
        // Gave up on syncing audio to video and instead sync video to audio
        if (_lastFrame == null)
        {
            _lastFrame = new Buffer<byte>((int)_bufferSize);
            _lastFrame.Zero();
        }

        Native.videoContextCopyRecentFrame(_context, Position, _lastFrame.GetPtr(), _bufferSize);
        return _lastFrame.Copy();
    }

    public void SetSource(IVideoSource source)
    {
        // The decode thread may be reading the old source until native has switched to the new one.
        var previous = _source;
        var next = new InternalSource(source);
        _ready = false;
        _error = null;
        Volatile.Write(ref _pendingSeek, null);
        Native.videoContextSetSource(_context, next.NativeSource);
        _source = next;
        previous?.Dispose();

        lock (_audioLock)
        {
            DropAudioStream();
            _videoPlaybackTime.SetTime(0.0);
        }

        _lastFrame?.Dispose();
        _lastFrame = null;
        Duration = 0.0;
        _bufferSize = 0;

        // Only the decode thread touches a source, so even a file becomes ready there a moment later.
        _decodeEvent.Set();
    }

    public void Dispose()
    {
        ReleaseResources();
        GC.SuppressFinalize(this);
    }

    private void DecodeLoop()
    {
        while (!_stopDecode)
        {
            if (!Step()) _decodeEvent.WaitOne();
        }
    }

    // One unit of work for the decode thread. True when there may be more to do right now.
    private bool Step()
    {
        if (_source is not { } source || _error is not null) return false;
        if (source.Error is { } sourceError)
        {
            _error = sourceError;
            return false;
        }

        if (!_ready) return TryBecomeReady();
        if (Volatile.Read(ref _pendingSeek) is { } seek) return PerformSeek(seek);
        if (!ShouldDecode()) return false;
        return Continue(Native.videoContextDecode(_context, DecodeStepSeconds));
    }

    private bool TryBecomeReady()
    {
        var result = Native.videoContextEnsureHeader(_context);
        if (result != NativeResult.Success) return Continue(result);
        if (_ready) return true;

        Duration = Native.videoContextGetDuration(_context);
        if (Native.videoContextHasVideo(_context) == 1)
        {
            var extent = VideoExtent;
            _bufferSize = extent.Width * extent.Height * 4;
            var estimatedBytesPerFrame = System.Math.Max(1.0, extent.Width * (double)extent.Height * 1.5);
            _maxBufferedFrames = System.Math.Max(MinBufferedFrames, (int)(DecodeBufferBudgetBytes / estimatedBytesPerFrame));
        }

        if (Native.videoContextHasAudio(_context) == 1)
        {
            AudioSampleRate = Native.videoContextGetAudioSampleRate(_context);
            AudioChannels = Native.videoContextGetAudioChannels(_context);
        }

        _ready = true;
        return true;
    }

    private bool PerformSeek(SeekRequest seek)
    {
        var result = Native.videoContextSeek(_context, seek.Position);
        if (result == NativeResult.Success)
        {
            lock (_audioLock)
            {
                // Audio from here on starts at the new position, so nothing pushed before the seek may linger.
                DropAudioStream();
                Interlocked.CompareExchange(ref _pendingSeek, null, seek);
            }
        }

        return Continue(result);
    }

    // Under _audioLock.
    private void DropAudioStream()
    {
        _audioStream?.Dispose();
        _audioStream = null;
    }

    // Whether the decode thread should go straight on after a native step, and records a rejected source.
    private bool Continue(int result)
    {
        if (result == NativeResult.Error) _error = new VideoDecodeException("The decoder rejected the source");
        return result == NativeResult.Success;
    }

    private bool ShouldDecode()
    {
        var position = Position;
        // Frames pile up when nothing is drawing them, so drop the ones playback has passed.
        Native.videoContextDropFramesBefore(_context, position);
        return DecodedPosition - position <= DecodeAheadSeconds &&
               Native.videoContextGetBufferedFrameCount(_context) < _maxBufferedFrames;
    }

    [UnmanagedCallersOnly]
    private static unsafe void OnAudioCallback(float* data, int count, double time, IntPtr context)
    {
        var handle = GCHandle.FromIntPtr(context);
        if (handle.Target is AudioCallbackHandler audioCallbackHandler)
        {
            audioCallbackHandler.Callback(new ReadOnlySpan<byte>(data, count * sizeof(float)), time);
        }
    }

    private void AudioCallback(ReadOnlySpan<byte> data, double time)
    {
        lock (_audioLock)
        {
            // Packets still coming out of the old position while a seek waits are not worth hearing.
            if (Volatile.Read(ref _pendingSeek) is not null) return;
            if (_audioStream == null)
            {
                _audioPacketsStartAt = time;
                _audioStream = _createStream?.Invoke(AudioSampleRate, AudioChannels) ??
                               _audioModule.MasterAudioGroup.CreatePushStream(AudioSampleRate, AudioChannels);
                if (IsPlaying) _audioStream.Play();
            }

            _audioStream.Push(data);
        }
    }

    private void ReleaseResources()
    {
        if (_context == IntPtr.Zero) return;
        _stopDecode = true;
        _decodeEvent.Set();
        _decodeTask.Wait();
        lock (_audioLock)
        {
            DropAudioStream();
        }

        _lastFrame?.Dispose();
        _lastFrame = null;
        _audioCallbackHandlerGcHandle.Free();
        Native.videoContextFree(_context);
        _context = IntPtr.Zero;
        _source?.Dispose();
        _source = null;
    }

    ~WebmVideoPlayer()
    {
        ReleaseResources();
    }

    private static class NativeResult
    {
        public const int Error = -1;
        public const int IncompleteHeader = 0;
        public const int IncompleteCluster = 1;
        public const int Success = 2;
        public const int Finished = 3;
    }

    private sealed record SeekRequest(double Position);

    private sealed class AudioCallbackHandler(Action<ReadOnlySpan<byte>, double> callback)
    {
        public readonly Action<ReadOnlySpan<byte>, double> Callback = callback;
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
                NativeSource = Native.videoSourceCreate(&callbacks);
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
