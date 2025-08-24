using Rin.Framework.Graphics;

namespace Rin.Framework.Video;

/// <summary>
///     Handles the generation of video frames and playing of audio
/// </summary>
public interface IVideoPlayer : IDisposable
{
    /// <summary>
    ///     The position the decoder is at
    /// </summary>
    public double DecodedPosition { get; }

    /// <summary>
    ///     The playback position
    /// </summary>
    public double Position { get; }

    public double Duration { get; }
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