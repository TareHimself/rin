using System.Numerics;

namespace rin.Framework.Core.Animation;

public abstract class TransitionAnimation<TValue>(Func<TValue> from,TValue to,Action<TValue> setter,double duration = 0.0,Func<double,double>? easingFunction = null) : IAnimation
{
    public double Duration => duration;
    private TValue _from = default!;

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
        
        var diff = Subtract(to,_from);
        var delta = ApplyAlpha(diff, alpha);
        setter(Add(_from,delta));
    }

    protected abstract TValue ApplyAlpha(in TValue diff, double alpha);

    protected abstract TValue Add(in TValue a,in TValue b);
    
    protected abstract TValue Subtract(in TValue a, in TValue b);
}