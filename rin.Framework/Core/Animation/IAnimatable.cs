namespace rin.Framework.Core.Animation;

/// <summary>
///     interface for anything that can be animated
/// </summary>
public interface IAnimatable
{
    public AnimationRunner AnimationRunner { get; init; }

    public void Update()
    {
        AnimationRunner.Update();
    }
}