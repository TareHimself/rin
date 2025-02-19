using System.Numerics;

namespace rin.Framework.Core.Animation;

public class GenericTransitionAnimation<TValue>(
    Func<TValue> from,
    TValue to,
    Action<TValue> setter,
    double duration = 0.0,
    Func<double, double>? easingFunction = null)
    : TransitionAnimation<TValue>(from, to, setter, duration, easingFunction)
    where TValue : IAdditionOperators<TValue, TValue, TValue>, ISubtractionOperators<TValue, TValue, TValue>,
    IMultiplyOperators<TValue, float, TValue>
{
    protected override TValue ApplyAlpha(in TValue diff, double alpha)
    {
        return diff * (float)alpha;
    }

    protected override TValue Add(in TValue a, in TValue b)
    {
        return a + b;
    }

    protected override TValue Subtract(in TValue a, in TValue b)
    {
        return a - b;
    }
}