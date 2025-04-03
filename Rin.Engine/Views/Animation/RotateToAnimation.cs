using Rin.Engine.Animation;

namespace Rin.Engine.Views.Animation;

public class RotateToAnimation<T>(T view, float? from, float to, float duration = 0f) : IAnimation where T : View
{
    private float _from = from.GetValueOrDefault(view.Angle);
    public float Duration => duration;

    public void Start(float elapsed)
    {
        _from = from.GetValueOrDefault(view.Angle);
        Update(elapsed);
    }

    public void Update(float elapsed)
    {
        var alpha = Duration == 0.0 ? 1.0 : elapsed / Duration;

        view.Angle = _from + (to - _from) * (float)alpha;
    }
}