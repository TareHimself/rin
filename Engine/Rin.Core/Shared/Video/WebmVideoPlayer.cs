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
    // Byte budget (not a fixed lookahead) so buffered decoded video stays roughly constant across resolutions.
    // Acts as a hard ceiling on top of the seconds-ahead target below, bounding memory if decode falls badly
    // behind playback.
    private const double DecodeBufferBudgetBytes = 32.0 * 1024 * 1024;
    private const int MinBufferedFrames = 3;
    private const int MaxBufferedFrames = 24;
    // Floor/ceiling on how many seconds of video a single decode call is asked to produce - the actual amount
    // requested each time is sized to close the (target ahead) - (already decoded ahead) gap, not fixed.
    private const double MinDecodeAheadSeconds = 0.15;
    private const double MaxDecodeAheadSeconds = 1.0;

    // Decode (packet -> RGBA) is the actual bottleneck, not the frame-count buffer, so the real throttle is how
    // far ahead of playback (in seconds) we let the decode thread get. Starts optimistic and adapts down as we
    // measure how long decoding actually takes relative to the video time it produces - a video that decodes
    // near-instantly doesn't need a big lookahead, one that barely keeps up with realtime does.
    private const double InitialTargetSecondsAhead = 2.0;
    private const double MinTargetSecondsAhead = 0.5;
    private const double DecodeCostSmoothing = 0.2;
    private double _decodeCostRatio = 1.0;
    private double _targetSecondsAhead = InitialTargetSecondsAhead;
    private double _pendingDecodeSeconds = MaxDecodeAheadSeconds;

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly IntPtr _context;

    private readonly IAudioModule _audioModule;
    private readonly Func<int, int, IPushStream>? _createStream;
    private readonly AutoResetEvent _decodeEvent = new(false);

    private readonly IChronometer _videoPlaybackTime = new Chronometer();
    private double _audioPacketsStartAt;
    private IPushStream? _audioStream;

    private ulong _bufferSize;
    private int _targetBufferedFrames = MinBufferedFrames;

    // Cached so CopyRecentFrame can return cheaply when called before a new frame decodes.
    private Buffer<byte>? _lastFrame;

    private InternalSource? _source;
    private bool _stopDecode;
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

        Task.Factory.StartNew(() =>
        {
            while (!_stopDecode)
            {
                while (DecodedPosition - Position <= 1)
                {
                    Native.videoContextDecode(_context, 0.1f);
                }
                _decodeEvent.WaitOne();
                // _decodeEvent.WaitOne();
                // if (_stopDecode)
                // {
                //     _source?.Dispose();
                //     Native.videoContextFree(_context);
                //     return;
                // }
                //
                // var newDecodePosition = DecodedPosition + _pendingDecodeSeconds;
                // var skipDecodeThreshold = Position + (_pendingDecodeSeconds * 2);
                // if (newDecodePosition > skipDecodeThreshold)
                // {
                //     continue;
                // }
                // var diff = DecodedPosition - Position;
                // var secondsToDecode = _pendingDecodeSeconds;
                // var stopwatch = Stopwatch.StartNew();
                // Native.videoContextDecode(_context, 0.5f);
                // var timeTaken = stopwatch.Elapsed.TotalSeconds;
                //
                // var ratio = timeTaken / secondsToDecode;
                // _pendingDecodeSeconds = double.Clamp(timeTaken * 1.1, MinDecodeAheadSeconds,
                //     MaxDecodeAheadSeconds);
                // _decodeCostRatio += (ratio - _decodeCostRatio) * DecodeCostSmoothing;
                // _targetSecondsAhead = System.Math.Clamp(InitialTargetSecondsAhead * _decodeCostRatio,
                //     MinTargetSecondsAhead, InitialTargetSecondsAhead);
            }
        });
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
        // var bufferedFrames = Native.videoContextGetBufferedFrameCount(_context);
        // if (bufferedFrames >= _targetBufferedFrames) return;
        // // Keep a frame-count floor regardless of the time-ahead target below - if decode is fast enough that
        // // the adaptive target has shrunk close to MinTargetSecondsAhead, we'd otherwise stop after buffering
        // // barely one frame, leaving no cushion against a stray slow decode or scheduling hiccup.
        // if (bufferedFrames < MinBufferedFrames)
        // {
        //     _pendingDecodeSeconds = MinDecodeAheadSeconds;
        //     _decodeEvent.Set();
        //     return;
        // }
        //
        // // How far ahead we already are vs. how far ahead we want to be - decode exactly that gap (clamped),
        // // instead of nibbling in fixed-size chunks regardless of how far behind we've fallen.
        // var secondsNeeded = _targetSecondsAhead - (DecodedPosition - Position);
        // if (secondsNeeded <= 0) return;
        // _pendingDecodeSeconds = System.Math.Clamp(secondsNeeded, MinDecodeAheadSeconds, MaxDecodeAheadSeconds);
        // _decodeEvent.Set();
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
        var ptr = Native.videoContextCopyRecentFrame(_context, Position);
        if (ptr != IntPtr.Zero)
        {
            _lastFrame?.Dispose();
            _lastFrame = new Buffer<byte>(ptr, _bufferSize);
        }

        if (_lastFrame != null) return _lastFrame.Copy();

        var blank = new Buffer<byte>((int)_bufferSize);
        blank.Zero();
        return blank;
    }


    public void SetSource(IVideoSource source)
    {
        _source?.Dispose();
        _source = new InternalSource(source);
        Native.videoContextSetSource(_context, _source.NativeSource);
        _decodeCostRatio = 1.0;
        _targetSecondsAhead = InitialTargetSecondsAhead;
        _bufferSize = VideoExtent.Width * VideoExtent.Height * 4;
        Duration = Native.videoContextGetDuration(_context);

        _lastFrame?.Dispose();
        _lastFrame = null;

        if (HasVideo)
        {
            var extent = VideoExtent;
            var estimatedBytesPerFrame = System.Math.Max(1.0, extent.Width * (double)extent.Height * 1.5);
            _targetBufferedFrames = System.Math.Clamp((int)(DecodeBufferBudgetBytes / estimatedBytesPerFrame),
                MinBufferedFrames, MaxBufferedFrames);
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
        _audioStream?.Dispose();
        _audioStream = null;
        _lastFrame?.Dispose();
        _lastFrame = null;
        _audioCallbackHandlerGcHandle.Free();
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