namespace rin.Framework.Core.Animation;

public interface IAnimation
{
    public double Duration { get; }
    public void Start(double elapsed);

    /// <summary>
    ///     Process This animation
    /// </summary>
    /// <param name="elapsed"></param>
    /// <returns>The time remaining i.e. float.PositiveInfinity for non finite animations</returns>
    public void Update(double elapsed);
}