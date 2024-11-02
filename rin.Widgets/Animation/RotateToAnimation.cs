using rin.Core.Animation;

namespace rin.Widgets.Animation;

public class RotateToAnimation<T>(T widget,float from,float to,double duration = 0.0) : IAnimation where T : Widget
{
    public double Duration => duration;
    public void DoUpdate(double start, double current)
    {
        var elapsed = current - start;
        
        var alpha = Duration == 0.0 ? 1.0 : (elapsed / Duration);

        widget.Angle = from + ((to - from) * (float)alpha);
    }

    public IAnimation? Next { get; set; }
}