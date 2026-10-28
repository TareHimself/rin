using System.Diagnostics;
using Rin.Core.Audio;
using Rin.Core.Graphics;
using Rin.Core.Shared.Buffers;
using Rin.Core.Shared.Time;

namespace Rin.Core.Shared.Video;

/// <summary>
///     Decodes webm video on another thread. The source may still be arriving: header reads, seeks and decodes that
///     run out of data are retried on every <see cref="TryDecode" />, so a view that calls it each frame keeps the
///     player going as bytes come in. All native work goes through <see cref="IWebmDemuxer" />; this type owns the
///     playback clock, the buffering decisions, seeks and the audio stream.
/// </summary>
public class WebmVideoPlayer : IVideoPlayer
{
    // Decoded frames wait as I420, 1.5 bytes per pixel, so this bounds the video held ahead of playback.
    private const double DecodeBufferBudgetBytes = 256.0 * 1024 * 1024;
    private const int MinBufferedFrames = 8;
    private const double DecodeAheadSeconds = 1.0;
    private const double DecodeStepSeconds = 0.1;

    private readonly IWebmDemuxer _demuxer;
    private readonly Func<int, int, IPushStream>? _createStream;
    private readonly AutoResetEvent _decodeEvent = new(false);
    // The audio stream is made on the decode thread and read on the main thread, and a seek replaces it.
    private readonly Lock _audioLock = new();
    private readonly Task? _decodeTask;

    private readonly IChronometer _videoPlaybackTime = new Chronometer();
    private IAudioModule? _audioModule;
    private double _audioPacketsStartAt;
    private IPushStream? _audioStream;

    private ulong _bufferSize;
    private int _maxBufferedFrames = MinBufferedFrames;

    // The frame on screen, which native overwrites in place when a newer one is due.
    private Buffer<byte>? _lastFrame;

    // A seek the decode thread still has to make, kept until the decoder has the data to land it.
    private SeekRequest? _pendingSeek;
    private volatile bool _hasSource;
    private volatile bool _ready;
    private volatile Exception? _error;
    private volatile bool _stopDecode;
    private bool _disposed;

    public WebmVideoPlayer(IAudioModule? audioModule = null)
        : this(onAudioPacket => new WebmDemuxer(onAudioPacket), null, audioModule, startDecodeThread: true)
    {
    }

    public WebmVideoPlayer(Func<int, int, IPushStream> createAudioStream, IAudioModule? audioModule = null)
        : this(onAudioPacket => new WebmDemuxer(onAudioPacket), createAudioStream, audioModule, startDecodeThread: true)
    {
    }

    /// <summary>
    ///     Tests hand in a scripted demuxer and drive <see cref="Step" /> themselves instead of starting the decode
    ///     thread.
    /// </summary>
    internal WebmVideoPlayer(Func<AudioPacketHandler, IWebmDemuxer> createDemuxer,
        Func<int, int, IPushStream>? createAudioStream, IAudioModule? audioModule, bool startDecodeThread)
    {
        _demuxer = createDemuxer(AudioCallback);
        _createStream = createAudioStream;
        _audioModule = audioModule;
        if (startDecodeThread) _decodeTask = Task.Factory.StartNew(DecodeLoop, TaskCreationOptions.LongRunning);
    }

    public bool IsReady => _ready;
    public bool HasFailed => _error is not null;
    public Exception? Error => _error;

    public double DecodedPosition => _demuxer.Position;

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

    public ByteRange NeededRange => _demuxer.NeededRange;

    public bool IsPlaying { get; private set; }
    // Gated on _ready so a caller never sees a track before the frame buffer size that goes with it.
    public bool HasVideo => _ready && _demuxer.HasVideo;
    public int VideoTracksCount => HasVideo ? 1 : 0;
    public int SelectedVideoTrackIndex => 0;
    public Extent2D VideoExtent => _demuxer.VideoExtent;
    public bool HasAudio => _ready && _demuxer.HasAudio;

    public int AudioTracksCount => _demuxer.AudioTrackCount;
    public int SelectedAudioTrackIndex => 0;
    public int AudioSampleRate { get; private set; }
    public int AudioChannels { get; private set; }

    public void TryDecode()
    {
        if (!_hasSource) return;
        _decodeEvent.Set();
    }

    public void Play()
    {
        if (!_hasSource) return;
        IsPlaying = true;
        lock (_audioLock)
        {
            _videoPlaybackTime.Start();
            _audioStream?.Play();
        }
    }

    public void Pause()
    {
        if (!_hasSource) return;
        IsPlaying = false;
        lock (_audioLock)
        {
            _videoPlaybackTime.Stop();
            _audioStream?.Pause();
        }
    }

    public void Seek(double position)
    {
        if (!_hasSource) return;
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

        _demuxer.CopyRecentFrame(Position, _lastFrame.GetPtr(), _bufferSize);
        return _lastFrame.Copy();
    }

    public void SetSource(IVideoSource source)
    {
        _ready = false;
        _error = null;
        Volatile.Write(ref _pendingSeek, null);
        _demuxer.SetSource(source);
        _hasSource = true;

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

    /// <summary>
    ///     One unit of work for the decode thread. True when there may be more to do right now.
    /// </summary>
    internal bool Step()
    {
        if (!_hasSource || _error is not null) return false;
        if (_demuxer.SourceError is { } sourceError)
        {
            _error = sourceError;
            return false;
        }

        if (!_ready) return TryBecomeReady();
        if (Volatile.Read(ref _pendingSeek) is { } seek) return PerformSeek(seek);
        if (!ShouldDecode()) return false;
        return Continue(_demuxer.Decode(DecodeStepSeconds));
    }

    private bool TryBecomeReady()
    {
        var result = _demuxer.EnsureHeader();
        if (result != VideoDemuxResult.Success) return Continue(result);
        if (_ready) return true;

        Duration = _demuxer.Duration;
        if (_demuxer.HasVideo)
        {
            _bufferSize = _demuxer.FrameByteSize;
            _maxBufferedFrames = System.Math.Max(MinBufferedFrames,
                (int)(DecodeBufferBudgetBytes / System.Math.Max(1.0, _bufferSize)));
        }

        if (_demuxer.HasAudio)
        {
            AudioSampleRate = _demuxer.AudioSampleRate;
            AudioChannels = _demuxer.AudioChannels;
        }

        _ready = true;
        return true;
    }

    private bool PerformSeek(SeekRequest seek)
    {
        var result = _demuxer.Seek(seek.Position);
        if (result == VideoDemuxResult.Success)
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

    // Whether the decode thread should go straight on after a demux step, and records a rejected source.
    private bool Continue(VideoDemuxResult result)
    {
        if (result == VideoDemuxResult.Error) _error = new VideoDecodeException("The decoder rejected the source");
        return result == VideoDemuxResult.Success;
    }

    private bool ShouldDecode()
    {
        var position = Position;
        // Frames pile up when nothing is drawing them, so drop the ones playback has passed.
        _demuxer.DropFramesBefore(position);
        return DecodedPosition - position <= DecodeAheadSeconds && _demuxer.BufferedFrameCount < _maxBufferedFrames;
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
                _audioStream = CreateAudioStream();
                if (IsPlaying) _audioStream.Play();
            }

            _audioStream.Push(data);
        }
    }

    private IPushStream CreateAudioStream()
    {
        if (_createStream is { } create) return create(AudioSampleRate, AudioChannels);
        _audioModule ??= IAudioModule.Get();
        return _audioModule.MasterAudioGroup.CreatePushStream(AudioSampleRate, AudioChannels);
    }

    private void ReleaseResources()
    {
        if (_disposed) return;
        _disposed = true;
        _stopDecode = true;
        _decodeEvent.Set();
        _decodeTask?.Wait();
        lock (_audioLock)
        {
            DropAudioStream();
        }

        _lastFrame?.Dispose();
        _lastFrame = null;
        _demuxer.Dispose();
    }

    ~WebmVideoPlayer()
    {
        ReleaseResources();
    }

    private sealed record SeekRequest(double Position);
}
