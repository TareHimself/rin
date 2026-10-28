using Rin.Core.Audio;
using Rin.Core.Graphics;
using Rin.Core.Shared.Video;

namespace Rin.Core.Tests.Shared.Video;

public class WebmVideoPlayerTests
{
    private ScriptedDemuxer _demuxer = null!;
    private List<RecordingPushStream> _streams = null!;
    private WebmVideoPlayer _player = null!;

    [SetUp]
    public void CreatePlayer()
    {
        _demuxer = new ScriptedDemuxer();
        _streams = [];
        _player = new WebmVideoPlayer(onAudio =>
        {
            _demuxer.OnAudioPacket = onAudio;
            return _demuxer;
        }, (rate, channels) =>
        {
            var stream = new RecordingPushStream(rate, channels);
            _streams.Add(stream);
            return stream;
        }, null, startDecodeThread: false);
    }

    [TearDown]
    public void DisposePlayer()
    {
        _player.Dispose();
        _demuxer.Dispose();
    }

    private void SetSourceAndBecomeReady()
    {
        _player.SetSource(new EmptySource());
        Assert.That(_player.Step(), Is.True);
    }

    [Test]
    public void WithoutASource_NothingHappens()
    {
        _player.Play();
        _player.Seek(3);

        Assert.Multiple(() =>
        {
            Assert.That(_player.Step(), Is.False);
            Assert.That(_player.IsPlaying, Is.False);
            Assert.That(_demuxer.Calls, Is.Empty);
        });
    }

    [Test]
    public void Step_WhileTheHeaderIsIncomplete_StaysNotReady()
    {
        _demuxer.HeaderResults.Enqueue(VideoDemuxResult.IncompleteHeader);
        _player.SetSource(new EmptySource());

        var more = _player.Step();

        Assert.Multiple(() =>
        {
            Assert.That(more, Is.False);
            Assert.That(_player.IsReady, Is.False);
            Assert.That(_player.HasVideo, Is.False);
            Assert.That(_demuxer.Calls, Is.EqualTo(new[] { "SetSource", "EnsureHeader" }));
        });
    }

    [Test]
    public void Step_OnceTheHeaderIsRead_PublishesDurationAndTrackFormats()
    {
        _demuxer.Duration = 12.5;
        _demuxer.HasVideo = true;
        _demuxer.HasAudio = true;
        _demuxer.AudioSampleRate = 48000;
        _demuxer.AudioChannels = 2;

        SetSourceAndBecomeReady();

        Assert.Multiple(() =>
        {
            Assert.That(_player.IsReady, Is.True);
            Assert.That(_player.Duration, Is.EqualTo(12.5));
            Assert.That(_player.HasVideo, Is.True);
            Assert.That(_player.HasAudio, Is.True);
            Assert.That(_player.AudioSampleRate, Is.EqualTo(48000));
            Assert.That(_player.AudioChannels, Is.EqualTo(2));
        });
    }

    [Test]
    public void Step_WhenTheSourceFailed_ReportsItsException()
    {
        _player.SetSource(new EmptySource());
        _demuxer.SourceError = new IOException("socket closed");

        var more = _player.Step();

        Assert.Multiple(() =>
        {
            Assert.That(more, Is.False);
            Assert.That(_player.HasFailed, Is.True);
            Assert.That(_player.Error, Is.SameAs(_demuxer.SourceError));
            Assert.That(_demuxer.Calls, Does.Not.Contain("EnsureHeader"));
        });
    }

    [Test]
    public void Step_WhenTheDecoderRejectsTheSource_FailsAndStops()
    {
        SetSourceAndBecomeReady();
        _demuxer.DecodeResults.Enqueue(VideoDemuxResult.Error);

        var more = _player.Step();

        Assert.Multiple(() =>
        {
            Assert.That(more, Is.False);
            Assert.That(_player.Error, Is.TypeOf<VideoDecodeException>());
            Assert.That(_player.Step(), Is.False);
        });
    }

    [Test]
    public void Step_Decodes_UntilEnoughIsBufferedAhead()
    {
        SetSourceAndBecomeReady();

        Assert.That(_player.Step(), Is.True);
        _demuxer.Position = 1.5;

        Assert.Multiple(() =>
        {
            Assert.That(_player.Step(), Is.False);
            Assert.That(_demuxer.Calls.Count(call => call == "Decode"), Is.EqualTo(1));
        });
    }

    [Test]
    public void Step_StopsDecoding_WhenTheFrameBufferIsFull()
    {
        SetSourceAndBecomeReady();
        _demuxer.BufferedFrameCount = 8;

        Assert.Multiple(() =>
        {
            Assert.That(_player.Step(), Is.False);
            Assert.That(_demuxer.Calls, Does.Not.Contain("Decode"));
        });
    }

    [Test]
    public void Step_DropsFramesPlaybackHasPassed()
    {
        SetSourceAndBecomeReady();

        _player.Step();

        Assert.That(_demuxer.Calls, Does.Contain("DropFramesBefore"));
    }

    [Test]
    public void Seek_IsReportedAsThePositionUntilTheDemuxerLandsIt()
    {
        SetSourceAndBecomeReady();
        _demuxer.SeekResults.Enqueue(VideoDemuxResult.IncompleteCluster);

        _player.Seek(7);
        var pendingMore = _player.Step();
        var positionWhilePending = _player.Position;
        var landedMore = _player.Step();

        Assert.Multiple(() =>
        {
            Assert.That(pendingMore, Is.False);
            Assert.That(positionWhilePending, Is.EqualTo(7));
            Assert.That(landedMore, Is.True);
            Assert.That(_demuxer.Calls.Count(call => call == "Seek(7)"), Is.EqualTo(2));
            Assert.That(_player.Position, Is.EqualTo(7).Within(0.5));
        });
    }

    [Test]
    public void AudioPacket_OpensAStreamWithTheTrackFormatAndPushesTheData()
    {
        _demuxer.HasAudio = true;
        _demuxer.AudioSampleRate = 44100;
        _demuxer.AudioChannels = 1;
        SetSourceAndBecomeReady();
        _player.Play();

        _demuxer.OnAudioPacket([1, 2, 3, 4], 2.0);

        Assert.That(_streams, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(_streams[0].SampleRate, Is.EqualTo(44100));
            Assert.That(_streams[0].Channels, Is.EqualTo(1));
            Assert.That(_streams[0].IsPlaying, Is.True);
            Assert.That(_streams[0].Pushed, Is.EqualTo(new[] { new byte[] { 1, 2, 3, 4 } }));
        });
    }

    [Test]
    public void Position_FollowsTheAudioStreamOffsetByTheFirstPacketTime()
    {
        SetSourceAndBecomeReady();
        _demuxer.OnAudioPacket([0], 2.0);
        _streams[0].Position = 0.5;

        Assert.That(_player.Position, Is.EqualTo(2.5));
    }

    [Test]
    public void AudioPacket_WhileASeekIsPending_IsIgnored()
    {
        SetSourceAndBecomeReady();
        _demuxer.SeekResults.Enqueue(VideoDemuxResult.IncompleteCluster);
        _player.Seek(3);
        _player.Step();

        _demuxer.OnAudioPacket([0], 3.0);

        Assert.That(_streams, Is.Empty);
    }

    [Test]
    public void Seek_DropsTheAudioStream_AndTheLandedSeekStartsANewOne()
    {
        SetSourceAndBecomeReady();
        _demuxer.OnAudioPacket([0], 0.0);

        _player.Seek(5);
        _player.Step();
        _demuxer.OnAudioPacket([0], 5.0);

        Assert.Multiple(() =>
        {
            Assert.That(_streams, Has.Count.EqualTo(2));
            Assert.That(_streams[0].Disposed, Is.True);
            Assert.That(_streams[1].Disposed, Is.False);
        });
    }

    [Test]
    public void PlayAndPause_FollowThroughToTheAudioStream()
    {
        SetSourceAndBecomeReady();
        _demuxer.OnAudioPacket([0], 0.0);

        _player.Play();
        var playing = _streams[0].IsPlaying;
        _player.Pause();

        Assert.Multiple(() =>
        {
            Assert.That(playing, Is.True);
            Assert.That(_streams[0].IsPlaying, Is.False);
            Assert.That(_player.IsPlaying, Is.False);
        });
    }

    [Test]
    public void CopyRecentFrame_AsksForTheFrameAtThePlaybackPosition()
    {
        _demuxer.HasVideo = true;
        _demuxer.FrameByteSize = 6;
        SetSourceAndBecomeReady();

        using var frame = _player.CopyRecentFrame();

        Assert.Multiple(() =>
        {
            Assert.That(frame.ElementCount, Is.EqualTo(6));
            Assert.That(_demuxer.Calls, Does.Contain("CopyRecentFrame(6)"));
        });
    }

    [Test]
    public void Dispose_DisposesTheDemuxerAndTheAudioStreamOnce()
    {
        SetSourceAndBecomeReady();
        _demuxer.OnAudioPacket([0], 0.0);

        _player.Dispose();
        _player.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(_demuxer.Disposed, Is.EqualTo(1));
            Assert.That(_streams[0].Disposed, Is.True);
        });
    }

    private sealed class EmptySource : IVideoSource
    {
        public long Available => 0;
        public long Length => 0;

        public void Read(long offset, Span<byte> destination)
        {
        }

        public long AvailableFrom(long offset) => offset;

        public void MakeRangeAvailable(long offset, long size)
        {
        }

        public void ReleaseBefore(long offset)
        {
        }

        public void Dispose()
        {
        }
    }

    private sealed class ScriptedDemuxer : IWebmDemuxer
    {
        public List<string> Calls { get; } = [];
        public Queue<VideoDemuxResult> HeaderResults { get; } = new();
        public Queue<VideoDemuxResult> DecodeResults { get; } = new();
        public Queue<VideoDemuxResult> SeekResults { get; } = new();
        public AudioPacketHandler OnAudioPacket { get; set; } = null!;
        public int Disposed { get; private set; }

        public Exception? SourceError { get; set; }
        public bool HasVideo { get; set; }
        public bool HasAudio { get; set; }
        public double Duration { get; set; }
        public double Position { get; set; }
        public Extent2D VideoExtent { get; set; }
        public ulong FrameByteSize { get; set; }
        public int AudioSampleRate { get; set; }
        public int AudioChannels { get; set; }
        public int AudioTrackCount { get; set; }
        public ByteRange NeededRange { get; set; }
        public int BufferedFrameCount { get; set; }

        public void SetSource(IVideoSource source) => Calls.Add("SetSource");

        public VideoDemuxResult EnsureHeader() => Next("EnsureHeader", HeaderResults);

        public VideoDemuxResult Decode(double stepSeconds) => Next("Decode", DecodeResults);

        public VideoDemuxResult Seek(double seconds) => Next($"Seek({seconds})", SeekResults);

        public void DropFramesBefore(double seconds) => Calls.Add("DropFramesBefore");

        public void CopyRecentFrame(double time, IntPtr destination, ulong size) =>
            Calls.Add($"CopyRecentFrame({size})");

        public void Dispose() => Disposed++;

        private VideoDemuxResult Next(string call, Queue<VideoDemuxResult> results)
        {
            Calls.Add(call);
            return results.Count > 0 ? results.Dequeue() : VideoDemuxResult.Success;
        }
    }

    private sealed class RecordingPushStream(int sampleRate, int channels) : IPushStream
    {
        public int SampleRate { get; } = sampleRate;
        public int Channels { get; } = channels;
        public List<byte[]> Pushed { get; } = [];
        public bool Disposed { get; private set; }

        public bool IsPlaying { get; private set; }
        public bool HasEnded => false;
        public double Position { get; set; }
        public double Duration => 0;

        public bool Play(bool restart = false)
        {
            IsPlaying = true;
            return true;
        }

        public bool Pause()
        {
            IsPlaying = false;
            return true;
        }

        public bool SetVolume(float value) => true;

        public bool SetPosition(double position)
        {
            Position = position;
            return true;
        }

        public void Push(in ReadOnlySpan<byte> data) => Pushed.Add(data.ToArray());

        public void Dispose() => Disposed = true;
    }
}
