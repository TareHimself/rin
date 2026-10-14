using System.Diagnostics;
using System.Runtime.InteropServices;
using Rin.Core.Audio;
using Rin.Core.Graphics;
using Rin.Core.Shared.Buffers;
using Rin.Core.Shared.Time;

namespace Rin.Core.Shared.Video;

/// <summary>
///     Decodes webm video on another thread.
/// </summary>
public class WebmVideoPlayer : IVideoPlayer
{
    // Decoded frames wait as I420, 1.5 bytes per pixel, so this bounds the video held ahead of playback.
    private const double DecodeBufferBudgetBytes = 256.0 * 1024 * 1024;
    private const int MinBufferedFrames = 8;
    private const double DecodeAheadSeconds = 1.0;
    private const double DecodeStepSeconds = 0.1;

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly IntPtr _context;

    private readonly IAudioModule _audioModule;
    private readonly Func<int, int, IPushStream>? _createStream;
    private readonly AutoResetEvent _decodeEvent = new(false);

    private readonly IChronometer _videoPlaybackTime = new Chronometer();
    private double _audioPacketsStartAt;
    private IPushStream? _audioStream;

    private ulong _bufferSize;
    private int _maxBufferedFrames = MinBufferedFrames;

    // The frame on screen, which native overwrites in place when a newer one is due.
    private Buffer<byte>? _lastFrame;

    private InternalSource? _source;
    private volatile bool _stopDecode;
    private readonly Task _decodeTask;
    private GCHandle _audioCallbackHandlerGcHandle;
    
    private class AudioCallbackHandler(Action<ReadOnlySpan<byte>, double> callback)
    {
        public readonly Action<ReadOnlySpan<byte>, double> Callback = callback;
    }

    [UnmanagedCallersOnly]
    private static unsafe void OnAudioCallback(float* data, int count, double time, IntPtr context)
    {
        var handle = GCHandle.FromIntPtr(context);
        if (handle.Target is AudioCallbackHandler audioCallbackHandler)
        {
            audioCallbackHandler.Callback(new ReadOnlySpan<byte>(data,count *  sizeof(float)), time);
        }
    }

    public WebmVideoPlayer(IAudioModule? audioModule = null)
    {
        _audioModule = audioModule ?? IAudioModule.Get();
        _context = Native.videoContextCreate();
        unsafe
        {
            var handler = new AudioCallbackHandler(AudioCallback);
            _audioCallbackHandlerGcHandle  = GCHandle.Alloc(handler, GCHandleType.Normal);
            try
            {
                Native.videoContextSetAudioCallback(_context, &OnAudioCallback,GCHandle.ToIntPtr(_audioCallbackHandlerGcHandle));
            }
            catch (Exception e)
            {
                _audioCallbackHandlerGcHandle.Free();
                Console.WriteLine(e);
                throw;
            }
        }

        _decodeTask = Task.Factory.StartNew(DecodeLoop, TaskCreationOptions.LongRunning);
    }

    private void DecodeLoop()
    {
        while (!_stopDecode)
        {
            if (!ShouldDecode() || Native.videoContextDecode(_context, DecodeStepSeconds) == 0)
            {
                _decodeEvent.WaitOne();
            }
        }
    }

    private bool ShouldDecode()
    {
        if (_source == null) return false;
        var position = Position;
        // Frames pile up when nothing is drawing them, so drop the ones playback has passed.
        Native.videoContextDropFramesBefore(_context, position);
        return DecodedPosition - position <= DecodeAheadSeconds &&
               Native.videoContextGetBufferedFrameCount(_context) < _maxBufferedFrames;
    }

    public WebmVideoPlayer(Func<int, int, IPushStream> createAudioStream, IAudioModule? audioModule = null) : this(audioModule)
    {
        _createStream = createAudioStream;
    }

    public double DecodedPosition => Native.videoContextGetPosition(_context);

    public double Position => _audioStream is { } audio
        ? audio.Position + _audioPacketsStartAt
        : _videoPlaybackTime.TotalSeconds;

    public double Duration { get; set; }

    public bool IsPlaying { get; private set; }
    public bool HasVideo => Native.videoContextHasVideo(_context) == 1;
    public int VideoTracksCount { get; set; }
    public int SelectedVideoTrackIndex => 0;
    public Extent2D VideoExtent => Native.videoContextGetVideoExtent(_context);
    public bool HasAudio => Native.videoContextHasAudio(_context) == 1;

    public int AudioTracksCount => Native.videoContextGetAudioTrackCount(_context);
    public int SelectedAudioTrackIndex => 0;
    public int AudioSampleRate { get; set; }
    public int AudioChannels { get; set; }


    public void TryDecode()
    {
        if (_source == null) return;
        _decodeEvent.Set();
    }

    public void Play()
    {
        if (_source == null) return;
        IsPlaying = true;
        _videoPlaybackTime.Start();
        _audioStream?.Play();
    }

    public void Pause()
    {
        if (_source == null) return;
        IsPlaying = false;
        _videoPlaybackTime.Stop();
        _audioStream?.Pause();
    }

    public void Seek(double position)
    {
        if (_source == null) return;
        _videoPlaybackTime.SetTime(position);
        _audioStream?.Dispose();
        _audioStream = null;
        Native.videoContextSeek(_context, position);
        _lastFrame?.Dispose();
        _lastFrame = null;
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
        Native.videoContextSetSource(_context, next.NativeSource);
        _source = next;
        previous?.Dispose();
        _bufferSize = VideoExtent.Width * VideoExtent.Height * 4;
        Duration = Native.videoContextGetDuration(_context);

        _lastFrame?.Dispose();
        _lastFrame = null;

        if (HasVideo)
        {
            var extent = VideoExtent;
            var estimatedBytesPerFrame = System.Math.Max(1.0, extent.Width * (double)extent.Height * 1.5);
            _maxBufferedFrames = System.Math.Max(MinBufferedFrames,
                (int)(DecodeBufferBudgetBytes / estimatedBytesPerFrame));
        }

        if (HasAudio)
        {
            AudioSampleRate = Native.videoContextGetAudioSampleRate(_context);
            AudioChannels = Native.videoContextGetAudioChannels(_context);
        }
    }

    public void Dispose()
    {
        ReleaseResources();
        GC.SuppressFinalize(this);
    }

    private unsafe void AudioCallback(ReadOnlySpan<byte> data, double time)
    {
        if (_audioStream == null)
        {
            _audioPacketsStartAt = time;
            _audioStream = _createStream?.Invoke(AudioSampleRate, AudioChannels) ??
                           _audioModule.MasterAudioGroup.CreatePushStream(AudioSampleRate, AudioChannels);
            if (IsPlaying) _audioStream.Play();
        }

        _audioStream.Push(data);
    }

    private void ReleaseResources()
    {
        _stopDecode = true;
        _decodeEvent.Set();
        _decodeTask.Wait();
        _audioStream?.Dispose();
        _audioStream = null;
        _lastFrame?.Dispose();
        _lastFrame = null;
        _audioCallbackHandlerGcHandle.Free();
        Native.videoContextFree(_context);
        _source?.Dispose();
        _source = null;
    }

    ~WebmVideoPlayer()
    {
        ReleaseResources();
    }

    private class InternalSource : IDisposable
    {
        public readonly IntPtr NativeSource;
        public readonly IVideoSource VideoSource;
        private GCHandle _handle;

        public InternalSource(IVideoSource source)
        {
            _handle = GCHandle.Alloc(source, GCHandleType.Normal);
            unsafe
            {
                NativeSource = Native.videoSourceCreate(&Read, &Available, &Length,GCHandle.ToIntPtr(_handle));
            }
            VideoSource = source;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [UnmanagedCallersOnly]
        private static void Read(ulong offset, ulong size, IntPtr destination,IntPtr context)
        {
            
            unsafe
            {
                var handle = GCHandle.FromIntPtr(context);
                if (handle.Target is IVideoSource source)
                {
                    source.Read(offset, new Span<byte>(destination.ToPointer(), (int)size));
                }
            }
        }

        [UnmanagedCallersOnly]
        private static ulong Available(IntPtr context)
        {
            var handle = GCHandle.FromIntPtr(context);
            if (handle.Target is IVideoSource source)
            {
                return source.Available;
            }
            return 0;
        }

        [UnmanagedCallersOnly]
        private static ulong Length(IntPtr context)
        {
            var handle = GCHandle.FromIntPtr(context);
            if (handle.Target is IVideoSource source)
            {
                return source.Length;
            }
            return 0;
        }

        private void ReleaseUnmanagedResources()
        {
            Native.videoSourceFree(NativeSource);
            _handle.Free();
        }

        private void Dispose(bool disposing)
        {
            ReleaseUnmanagedResources();
        }

        ~InternalSource()
        {
            Dispose(false);
        }
    }
}