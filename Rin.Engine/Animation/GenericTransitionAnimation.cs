using System.Numerics;

namespace Rin.Engine.Animation;

public class GenericTransitionAnimation<TValue>(
    Func<TValue> from,
    TValue to,
    Action<TValue> setter,
    float duration = 0f,
    Func<float, float>? easingFunction = null)
    : TransitionAnimation<TValue>(from, to, setter, duration, easingFunction)
    where TValue : IAdditionOperators<TValue, TValue, TValue>, ISubtractionOperators<TValue, TValue, TValue>,
    IMultiplyOperators<TValue, float, TValue>
{
    protected override TValue ApplyAlpha(in TValue diff, float alpha)
    {
        return diff * alpha;
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