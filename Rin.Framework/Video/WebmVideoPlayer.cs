using System.Diagnostics;
using Rin.Framework.Audio;
using Rin.Framework.Buffers;
using Rin.Framework.Graphics;
using Rin.Framework.Shared.Time;

namespace Rin.Framework.Video;

/// <summary>
///     Decodes webm video on another thread.
/// </summary>
public class WebmVideoPlayer : IVideoPlayer
{
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly Native.Video.AudioCallbackDelegate _audioDelegate;
    private readonly IntPtr _context;
    private readonly AutoResetEvent _decodeEvent = new(false);
    private readonly ManualResetEvent _decodeFinishedEvent = new(true);

    private readonly IChronometer _videoPlaybackTime = new Chronometer();
    private double _audioPacketsStartAt;
    private IPushStream? _audioStream;

    private ulong _bufferSize;

    private InternalSource? _source;
    private bool _stopDecode;

    private readonly Func<int,int,IPushStream>? _createStream;
    public WebmVideoPlayer()
    {
        _context = Native.Video.ContextCreate();
        unsafe
        {
            _audioDelegate = AudioCallback;
            Native.Video.ContextSetAudioCallback(_context, _audioDelegate);
        }

        Task.Factory.StartNew(() =>
        {
            while (!_stopDecode)
            {
                _decodeEvent.WaitOne();
                if (_stopDecode)
                {
                    _source?.Dispose();
                    Native.Video.ContextFree(_context);
                    return;
                }

                _decodeFinishedEvent.Reset();
                Native.Video.ContextDecode(_context, 0.5);
                _decodeFinishedEvent.Set();
            }
        });
    }

    public WebmVideoPlayer(Func<int,int,IPushStream> createAudioStream) : this()
    {
        _createStream = createAudioStream;
    }

    public double DecodedPosition => Native.Video.ContextGetPosition(_context);

    public double Position => _audioStream is { } audio
        ? audio.Position + _audioPacketsStartAt
        : _videoPlaybackTime.TotalSeconds;

    public double Duration { get; set; }

    public bool IsPlaying { get; private set; }
    public bool HasVideo => Native.Video.ContextHasVideo(_context) == 1;
    public int VideoTracksCount { get; set; }
    public int SelectedVideoTrackIndex => 0;
    public Extent2D VideoExtent => Native.Video.ContextGetVideoExtent(_context);
    public bool HasAudio => Native.Video.ContextHasAudio(_context) == 1;

    public int AudioTracksCount => Native.Video.ContextGetAudioTrackCount(_context);
    public int SelectedAudioTrackIndex => 0;
    public int AudioSampleRate { get; set; }
    public int AudioChannels { get; set; }


    public void TryDecode()
    {
        if (_source == null) return;
        var decodeDelta = DecodedPosition - Position;
        var thresh = 1.5;
        if (decodeDelta <= thresh) _decodeEvent.Set();
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
        _decodeFinishedEvent.WaitOne();
        _videoPlaybackTime.SetTime(position);
        _audioStream?.Dispose();
        _audioStream = null;
        Native.Video.ContextSeek(_context, position);
    }

    public Buffer<byte> CopyRecentFrame()
    {
        Debug.Assert(HasVideo);
        // Gave up on syncing audio to video and instead sync video to audio
        return new Buffer<byte>(Native.Video.ContextCopyRecentFrame(_context, Position), _bufferSize)
        {
            Track = true
        };
    }


    public void SetSource(IVideoSource source)
    {
        _source?.Dispose();
        _source = new InternalSource(source);
        Native.Video.ContextSetSource(_context, _source.NativeSource);
        _bufferSize = VideoExtent.Width * VideoExtent.Height * 4;
        Duration = Native.Video.ContextGetDuration(_context);

        if (HasAudio)
        {
            AudioSampleRate = Native.Video.ContextGetAudioSampleRate(_context);
            AudioChannels = Native.Video.ContextGetAudioChannels(_context);
        }
    }

    public void Dispose()
    {
        ReleaseResources();
        GC.SuppressFinalize(this);
    }

    private unsafe void AudioCallback(float* data, int count, double time)
    {
        if (_audioStream == null)
        {
            _audioPacketsStartAt = time;
            _audioStream = _createStream?.Invoke(AudioSampleRate,AudioChannels) ?? IAudioModule.Get().CreatePushStream(AudioSampleRate, AudioChannels);
            if (IsPlaying) _audioStream.Play();
        }

        _audioStream.Push(new ReadOnlySpan<byte>(data, sizeof(float) * count));
    }

    private void ReleaseResources()
    {
        _stopDecode = true;
        _decodeEvent.Set();
        _audioStream?.Dispose();
        _audioStream = null;
    }

    ~WebmVideoPlayer()
    {
        ReleaseResources();
    }

    private class InternalSource : IDisposable
    {
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly Native.Video.SourceAvailableCallbackDelegate _availableDelegate;

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly Native.Video.SourceLengthCallbackDelegate _lengthDelegate;

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly Native.Video.SourceReadCallbackDelegate _readDelegate;
        public readonly IntPtr NativeSource;
        public readonly IVideoSource VideoSource;

        public InternalSource(IVideoSource source)
        {
            _readDelegate = Read;
            _availableDelegate = Available;
            _lengthDelegate = Length;
            NativeSource = Native.Video.SourceCreate(_readDelegate, _availableDelegate, _lengthDelegate);
            VideoSource = source;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Read(ulong offset, ulong size, IntPtr destination)
        {
            unsafe
            {
                VideoSource.Read(offset, new Span<byte>(destination.ToPointer(), (int)size));
            }
        }

        private ulong Available()
        {
            return VideoSource.Available;
        }

        private ulong Length()
        {
            return VideoSource.Length;
        }

        private void ReleaseUnmanagedResources()
        {
            Native.Video.SourceFree(NativeSource);
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