using System.Numerics;

namespace rin.Core.Animation;

public class SimpleTransitionAnimation<TValue>(Func<TValue> from,TValue to,Action<TValue> setter,double duration = 0.0,Func<double,double>? easingFunction = null) : IAnimation where TValue : ISubtractionOperators<TValue,TValue,TValue>,IMultiplyOperators<TValue,float,TValue>,IAdditionOperators<TValue,TValue,TValue>
{
    public double Duration => duration;
    private TValue _from;

    public void Start(double elapsed)
    {
        _from = from();
        Update(elapsed);
    }

    public void Update(double elapsed)
    {
        var alpha = Duration == 0.0 ? 1.0 : (elapsed / Duration);
        
        if (easingFunction != null)
        {
            alpha = easingFunction(alpha);
        }
        
        var diff = to - _from;
        var diffAlpha = diff * (float)alpha;
        setter(_from + diffAlpha);
    }
}