using System.Numerics;

namespace rin.Core.Animation;

public class SimpleTransitionAnimation<TValue>(TValue from,TValue to,Action<TValue> setter,double duration = 0.0,Func<double,double>? easingFunction = null) : IAnimation where TValue : ISubtractionOperators<TValue,TValue,TValue>,IMultiplyOperators<TValue,float,TValue>,IAdditionOperators<TValue,TValue,TValue>
{
    public double Duration => duration;
    public void DoUpdate(double start, double current)
    {
        var elapsed = current - start;
        
        var alpha = Duration == 0.0 ? 1.0 : (elapsed / Duration);
        
        if (easingFunction != null)
        {
            alpha = easingFunction(alpha);
        }
        
        var diff = to - from;
        var diffAlpha = diff * (float)alpha;
        setter(from + diffAlpha);
    }

    public IAnimation? Next { get; set; }
}