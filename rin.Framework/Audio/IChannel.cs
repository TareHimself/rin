namespace rin.Framework.Audio;

public interface IChannel : IDisposable
{
    public bool IsPlaying { get; }
    public bool IsPaused { get; }

    public double Position { get; }

    public double Length { get; }

    public bool Play(bool restart = false);

    public bool Pause();

    public bool SetVolume(float value);


    /// <summary>
    ///     Sets the position of this channel in seconds
    /// </summary>
    /// <param name="position">The new position</param>
    /// <returns></returns>
    public bool SetPosition(double position);
}