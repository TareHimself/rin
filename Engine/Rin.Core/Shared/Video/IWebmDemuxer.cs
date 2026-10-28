using Rin.Core.Graphics;

namespace Rin.Core.Shared.Video;

internal delegate void AudioPacketHandler(ReadOnlySpan<byte> data, double time);

/// <summary>
///     What a demux, seek or header read came back with. Mirrors VideoDemuxResult in native/Rin.Native/src/video.hpp.
/// </summary>
internal enum VideoDemuxResult
{
    Error = -1,
    IncompleteHeader = 0,
    IncompleteCluster = 1,
    Success = 2,
    Finished = 3
}

/// <summary>
///     The native webm decoder behind <see cref="WebmVideoPlayer" />, one export per member. The player decides when
///     to read the header, decode, seek, drop and present; the demuxer only does what it is told, so a scripted one
///     can drive the player in tests.
/// </summary>
internal interface IWebmDemuxer : IDisposable
{
    /// <summary>
    ///     An exception a source callback threw, kept here because nothing may unwind into native.
    /// </summary>
    Exception? SourceError { get; }

    bool HasVideo { get; }
    bool HasAudio { get; }
    double Duration { get; }
    double Position { get; }
    Extent2D VideoExtent { get; }
    ulong FrameByteSize { get; }
    int AudioSampleRate { get; }
    int AudioChannels { get; }
    int AudioTrackCount { get; }
    ByteRange NeededRange { get; }
    int BufferedFrameCount { get; }

    void SetSource(IVideoSource source);
    VideoDemuxResult EnsureHeader();
    VideoDemuxResult Decode(double stepSeconds);
    VideoDemuxResult Seek(double seconds);
    void DropFramesBefore(double seconds);
    void CopyRecentFrame(double time, IntPtr destination, ulong size);
}
