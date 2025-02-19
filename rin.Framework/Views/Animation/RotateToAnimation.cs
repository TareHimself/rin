using rin.Framework.Core.Animation;

namespace rin.Framework.Views.Animation;

public class RotateToAnimation<T>(T view, float? from, float to, double duration = 0.0) : IAnimation where T : View
{
    private float _from = from.GetValueOrDefault(view.Angle);
    public double Duration => duration;

    public void Start(double elapsed)
    {
        _from = from.GetValueOrDefault(view.Angle);
        Update(elapsed);
    }

    public void Update(double elapsed)
    {
        var alpha = Duration == 0.0 ? 1.0 : elapsed / Duration;

        view.Angle = _from + (to - _from) * (float)alpha;
    }
}