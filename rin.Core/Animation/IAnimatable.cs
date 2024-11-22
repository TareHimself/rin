namespace rin.Core.Animation;

/// <summary>
/// interface for anything that can be animated
/// </summary>
public interface IAnimatable
{
    public abstract AnimationRunner AnimationRunner { get; init; }

    public void Update()
    {
        AnimationRunner.Update();
    }
}