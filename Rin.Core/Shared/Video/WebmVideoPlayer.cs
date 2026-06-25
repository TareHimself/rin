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
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly IntPtr _context;

    private readonly Func<int, int, IPushStream>? _createStream;
    private readonly AutoResetEvent _decodeEvent = new(false);
    private readonly ManualResetEvent _decodeFinishedEvent = new(true);

    private readonly IChronometer _videoPlaybackTime = new Chronometer();
    private double _audioPacketsStartAt;
    private IPushStream? _audioStream;

    private ulong _bufferSize;

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

    public WebmVideoPlayer()
    {
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
                _decodeEvent.WaitOne();
                if (_stopDecode)
                {
                    _source?.Dispose();
                    Native.videoContextFree(_context);
                    return;
                }

                _decodeFinishedEvent.Reset();
                Native.videoContextDecode(_context, 0.5);
                _decodeFinishedEvent.Set();
            }
        });
    }

    public WebmVideoPlayer(Func<int, int, IPushStream> createAudioStream) : this()
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
        Native.videoContextSeek(_context, position);
    }

    public Buffer<byte> CopyRecentFrame()
    {
        Debug.Assert(HasVideo);
        // Gave up on syncing audio to video and instead sync video to audio
        return new Buffer<byte>(Native.videoContextCopyRecentFrame(_context, Position), _bufferSize);
    }


    public void SetSource(IVideoSource source)
    {
        _source?.Dispose();
        _source = new InternalSource(source);
        Native.videoContextSetSource(_context, _source.NativeSource);
        _bufferSize = VideoExtent.Width * VideoExtent.Height * 4;
        Duration = Native.videoContextGetDuration(_context);

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
                           IAudioModule.Get().MasterAudioGroup.CreatePushStream(AudioSampleRate, AudioChannels);
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