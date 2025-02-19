namespace rin.Framework.Core.Animation;

public class ActionAnimation(Action action) : IAnimation
{
    public double Duration => 0.0f;

    public void Start(double elapsed)
    {
        action();
    }

    public void Update(double elapsed)
    {
    }
}