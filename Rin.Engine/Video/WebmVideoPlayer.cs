using System.Diagnostics;
using Rin.Engine.Audio;
using Rin.Engine.Graphics;

namespace Rin.Engine.Video;
/// <summary>
/// Decodes webm video on another thread.
/// </summary>
public class WebmVideoPlayer : IVideoPlayer
{
    private readonly IntPtr _context;
    
    private InternalSource? _source;
    
    private ulong _bufferSize = 0;
    
    private readonly IChronometer _videoPlaybackTime = new Chronometer();
    public double DecodedPosition => Native.Video.ContextGetPosition(_context);
    public double Position => _audioStream is {} audio ? audio.Position + _audioPacketsStartAt : _videoPlaybackTime.TotalSeconds;
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
    public int AudioChannels  { get; set; }
    private IPushStream? _audioStream = null;
    private readonly AutoResetEvent _decodeEvent = new AutoResetEvent(false);
    private readonly ManualResetEvent _decodeFinishedEvent = new ManualResetEvent(true);
    private bool _stopDecode = false;
    private double _audioPacketsStartAt = 0;

    public WebmVideoPlayer()
    {
        _context = Native.Video.ContextCreate();
        unsafe
        {
            _audioDelegate = AudioCallback;
            Native.Video.ContextSetAudioCallback(_context,_audioDelegate);
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
                Native.Video.ContextDecode(_context, 2.0);
                _decodeFinishedEvent.Set();
            }
        });
    }

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly Native.Video.AudioCallbackDelegate _audioDelegate;
    
    private unsafe void AudioCallback(float* data, int count, double time)
    {
        
        if (_audioStream == null)
        {
            _audioPacketsStartAt = time;
            _audioStream = SAudioModule.Get().CreateAudioStream(AudioSampleRate,AudioChannels);
            if (IsPlaying)
            {
                _audioStream.Play();
            }
        }
        
        _audioStream.Push(new ReadOnlySpan<byte>(data,sizeof(float) * count));
    }

    
    public void TryDecode()
    {
        if(_source == null) return;
        var decodeDelta = (DecodedPosition -  Position);
        var thresh = 3;
        if (decodeDelta <= thresh)
        {
            _decodeEvent.Set();
        }
    }

    public void Play()
    {
        if(_source == null) return;
        IsPlaying = true;
        _videoPlaybackTime.Start();
        _audioStream?.Play();
    }

    public void Pause()
    {
        if(_source == null) return;
        IsPlaying = false;
        _videoPlaybackTime.Stop();
        _audioStream?.Pause();
    }

    public void Seek(double position)
    {
        if(_source == null) return;
        _decodeFinishedEvent.WaitOne();
        _videoPlaybackTime.SetTime(position);
        _audioStream?.Dispose();
        _audioStream = null;
        Native.Video.ContextSeek(_context,position);
    }

    public Buffer<byte> CopyRecentFrame()
    {
        Debug.Assert(HasVideo);
        // Gave up on syncing audio to video and instead sync video to audio
        return new Buffer<byte>(Native.Video.ContextCopyRecentFrame(_context,Position), _bufferSize)
        {
            Track = true
        };
    }
    
    
    
    public void SetSource(IVideoSource source)
    {
        _source?.Dispose();
        _source = new InternalSource(source);
        Native.Video.ContextSetSource(_context,_source.NativeSource);
        _bufferSize = VideoExtent.Width *VideoExtent.Height * 4;
        Duration =  Native.Video.ContextGetDuration(_context);
        
        if (HasAudio)
        {
            AudioSampleRate = Native.Video.ContextGetAudioSampleRate(_context);
            AudioChannels = Native.Video.ContextGetAudioChannels(_context);
        }
    }

    private void ReleaseUnmanagedResources()
    {
        _stopDecode = true;
        _decodeEvent.Set();
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~WebmVideoPlayer()
    {
        ReleaseUnmanagedResources();
    }

    private class InternalSource : IDisposable
    {
        public readonly IntPtr NativeSource;
        public readonly IVideoSource VideoSource;
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly Native.Video.SourceReadCallbackDelegate _readDelegate;
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly Native.Video.SourceAvailableCallbackDelegate _availableDelegate;
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly Native.Video.SourceLengthCallbackDelegate _lengthDelegate;
        
        public InternalSource(IVideoSource source)
        {
            _readDelegate = Read;
            _availableDelegate = Available;
            _lengthDelegate = Length;
            NativeSource = Native.Video.SourceCreate(_readDelegate, _availableDelegate, _lengthDelegate);
            VideoSource = source;
            
        }

        private void Read(ulong offset, ulong size, IntPtr destination)
        {
            unsafe
            {
                VideoSource.Read(offset,new Span<byte>(destination.ToPointer(),(int)size));
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~InternalSource()
        {
            Dispose(false);
        }
    }
}