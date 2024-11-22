namespace rin.Core.Animation;

public interface IAnimation
{

    public abstract double Duration { get; }
    public abstract void Start(double elapsed);

    /// <summary>
    /// Process This animation
    /// </summary>
    /// <param name="elapsed"></param>
    /// <returns>The time remaining i.e. float.PositiveInfinity for non finite animations</returns>
    public abstract void Update(double elapsed);
}