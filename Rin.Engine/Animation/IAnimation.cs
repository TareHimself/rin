namespace Rin.Engine.Animation;

public interface IAnimation
{
    public float Duration { get; }
    public void Start(float elapsed);

    /// <summary>
    ///     Process This animation
    /// </summary>
    /// <param name="elapsed"></param>
    /// <returns>The time remaining i.e. float.PositiveInfinity for non finite animations</returns>
    public void Update(float elapsed);
}