namespace Rin.Core.Audio;

public interface IChannel : IDisposable
{
    public bool IsPlaying { get; }

    /// <summary>
    ///     True once playback has run off the end of the sound. Seeking to the end does not set it, and
    ///     <see cref="Play" /> clears it and starts over.
    /// </summary>
    public bool HasEnded { get; }

    /// <summary>
    ///     The playback position in seconds, equal to <see cref="Duration" /> once <see cref="HasEnded" />.
    /// </summary>
    public double Position { get; }

    public double Duration { get; }

    public bool Play(bool restart = false);

    public bool Pause();

    public bool SetVolume(float value);


    /// <summary>
    ///     Sets the position of this channel in seconds
    /// </summary>
    /// <param name="position">The new position</param>
    /// <returns></returns>
    public bool SetPosition(double position);

    //public event Action? OnStart;
}