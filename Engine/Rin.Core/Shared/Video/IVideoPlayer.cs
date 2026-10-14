using Rin.Core.Graphics;
using Rin.Core.Shared.Buffers;

namespace Rin.Core.Shared.Video;

/// <summary>
///     Handles the generation of video frames and playing of audio
/// </summary>
public interface IVideoPlayer : IDisposable
{
    /// <summary>
    ///     True once the headers are read, so the tracks, extent and duration are known.
    /// </summary>
    public bool IsReady { get; }

    /// <summary>
    ///     True once the source or decoder has failed. <see cref="Error" /> says why.
    /// </summary>
    public bool HasFailed { get; }

    public Exception? Error { get; }

    /// <summary>
    ///     The position the decoder is at
    /// </summary>
    public double DecodedPosition { get; }

    /// <summary>
    ///     The playback position
    /// </summary>
    public double Position { get; }

    /// <summary>
    ///     Seconds, 0 while unknown as with a live stream.
    /// </summary>
    public double Duration { get; }

    /// <summary>
    ///     After the decoder ran out of data, the bytes the source is missing for its next step. Empty otherwise.
    /// </summary>
    public ByteRange NeededRange { get; }

    public bool IsPlaying { get; }
    public bool HasVideo { get; }
    public int VideoTracksCount { get; }
    public int SelectedVideoTrackIndex { get; }
    public Extent2D VideoExtent { get; }
    public bool HasAudio { get; }
    public int AudioTracksCount { get; }
    public int SelectedAudioTrackIndex { get; }
    public int AudioSampleRate { get; }
    public int AudioChannels { get; }
    public void TryDecode();
    public void Play();
    public void Pause();
    public void Seek(double position);
    public Buffer<byte> CopyRecentFrame();

    public void SetSource(IVideoSource source);
}
