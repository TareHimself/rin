using rin.Framework.Core.Animation;

namespace rin.Framework.Widgets.Animation;

public class RotateToAnimation<T>(T widget,float? from,float to,double duration = 0.0) : IAnimation where T : Widget
{
    private float _from = from.GetValueOrDefault(widget.Angle);
    public double Duration => duration;

    public void Start(double elapsed)
    {
        _from = from.GetValueOrDefault(widget.Angle);
        Update(elapsed);
    }

    public void Update(double elapsed)
    {
        var alpha = Duration == 0.0 ? 1.0 : (elapsed / Duration);

        widget.Angle = _from + ((to - _from) * (float)alpha);
    }
}