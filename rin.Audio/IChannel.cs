namespace rin.Audio;

public interface IChannel : IDisposable
{
    public abstract bool IsPlaying { get; }
    public abstract bool IsPaused { get; }
    
    public abstract double Position { get; }

    public abstract double Length { get; }

    public abstract bool Play(bool restart = false);

    public abstract bool Pause();

    public abstract bool SetVolume(float value);


    /// <summary>
    /// Sets the position of this channel in seconds
    /// </summary>
    /// <param name="position">The new position</param>
    /// <returns></returns>
    public abstract bool SetPosition(double position);
}