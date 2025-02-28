namespace Rin.Engine.Core.Animation;

public abstract class TransitionAnimation<TValue>(
    Func<TValue> from,
    TValue to,
    Action<TValue> setter,
    float duration = 0f,
    Func<float, float>? easingFunction = null) : IAnimation
{
    private TValue _from = default!;
    public float Duration => duration;

    public void Start(float elapsed)
    {
        _from = from();
        Update(elapsed);
    }

    public void Update(float elapsed)
    {
        var alpha = Duration == 0.0f ? 1.0f : elapsed / Duration;

        if (easingFunction != null) alpha = easingFunction(alpha);

        var diff = Subtract(to, _from);
        var delta = ApplyAlpha(diff, alpha);
        setter(Add(_from, delta));
    }

    protected abstract TValue ApplyAlpha(in TValue diff, float alpha);

    protected abstract TValue Add(in TValue a, in TValue b);

    protected abstract TValue Subtract(in TValue a, in TValue b);
}