namespace Rin.Framework;

/// <summary>
///     Double precision clock used to track time in seconds, supports starting, stopping and pausing
/// </summary>
public interface IChronometer
{
    /// <summary>
    ///     True if this <see cref="IChronometer" /> is recording time
    /// </summary>
    public bool IsRunning { get; }

    /// <summary>
    ///     Total time recorded by this <see cref="IChronometer" />
    /// </summary>
    public double TotalSeconds { get; }

    /// <summary>
    ///     Total time since the last call to <see cref="Start" />
    /// </summary>
    public double ElapsedTime { get; }

    /// <summary>
    ///     Starts recording time
    /// </summary>
    public void Start();

    /// <summary>
    ///     Stops recording time
    /// </summary>
    public void Stop();

    /// <summary>
    ///     Resets the <see cref="IChronometer" />. if it is currently recording it will reset <see cref="TotalSeconds" /> and
    ///     <see cref="ElapsedTime" /> but keep recording
    /// </summary>
    public void Reset();

    /// <summary>
    ///     Set the <see cref="TotalSeconds" /> to a specific time
    /// </summary>
    /// <param name="seconds"></param>
    public void SetTime(double seconds);
}