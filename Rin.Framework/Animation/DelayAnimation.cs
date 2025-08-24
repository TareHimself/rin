namespace Rin.Framework.Animation;

public class DelayAnimation(float duration) : IAnimation
{
    public float Duration => duration;

    public void Start(float elapsed)
    {
    }

    public void Update(float elapsed)
    {
    }
}