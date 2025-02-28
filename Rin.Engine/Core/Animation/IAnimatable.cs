namespace Rin.Engine.Core.Animation;

/// <summary>
///     interface for anything that can be animated
/// </summary>
public interface IAnimatable
{
    public AnimationRunner AnimationRunner { get; init; }

    public void UpdateRunner()
    {
        AnimationRunner.Update();
    }
}