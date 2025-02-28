namespace Rin.Engine.Core.Animation;

public class ActionAnimation(Action action) : IAnimation
{
    public float Duration => 0.0f;

    public void Start(float elapsed)
    {
        action();
    }

    public void Update(float elapsed)
    {
    }
}