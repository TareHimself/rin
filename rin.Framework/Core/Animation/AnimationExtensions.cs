using System.Numerics;

namespace rin.Framework.Core.Animation;

public static class AnimationExtensions
{
    /// <summary>
    ///     Stops all animations running on this <see cref="IAnimatable" />
    /// </summary>
    /// <param name="target"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T StopAll<T>(this T target) where T : IAnimatable
    {
        target.AnimationRunner.StopAll();
        return target;
    }

    /// <summary>
    ///     Creates a new <see cref="AnimationSequence{T}" />
    /// </summary>
    /// <param name="target"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static AnimationSequence<T> Sequence<T>(this T target) where T : IAnimatable
    {
        var seq = new AnimationSequence<T>(target);
        target.AddAnimation(seq);
        return seq;
    }

    public static IAnimation AddAnimation<T>(this T self, IAnimation anim) where T : IAnimatable
    {
        self.AnimationRunner.Add(anim);
        return anim;
    }

    /// <summary>
    ///     Will run the action as soon as possible
    /// </summary>
    /// <param name="self"></param>
    /// <param name="action"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static AnimationSequence<T> Do<T>(this T self, Action action) where T : IAnimatable
    {
        return self.Sequence().Do(action);
    }

    /// <summary>
    ///     Will run the action as soon as possible
    /// </summary>
    /// <param name="self"></param>
    /// <param name="action"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static AnimationSequence<T> Do<T>(this AnimationSequence<T> self, Action action) where T : IAnimatable
    {
        self.Add(new ActionAnimation(action));
        return self;
    }

    /// <summary>
    ///     Delay for the specified duration, will add an <see cref="AnimationSequence{T}.After" /> before and after the delay
    /// </summary>
    /// <param name="self"></param>
    /// <param name="duration"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static AnimationSequence<T> Delay<T>(this T self, double duration)
        where T : IAnimatable
    {
        return self.Sequence().Add(new DelayAnimation(duration)).After();
    }

    /// <summary>
    ///     Delay for the specified duration, will add an <see cref="AnimationSequence{T}.After" /> before and after the delay
    /// </summary>
    /// <param name="self"></param>
    /// <param name="duration"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static AnimationSequence<T> Delay<T>(this AnimationSequence<T> self, double duration)
        where T : IAnimatable
    {
        return self.After().Add(new DelayAnimation(duration)).After();
    }


    /// <summary>
    ///     Runs the <see cref="AnimationSequence{T}" /> after all the previous <see cref="IAnimation" />'s have completed
    /// </summary>
    /// <param name="self"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static AnimationSequence<T> After<T>(this T self) where T : IAnimatable
    {
        return self.AnimationRunner.After(self);
    }

    public static AnimationSequence<TTarget> Transition<TTarget, TValue>(this TTarget target, Func<TValue> getInitial,
        Action<TValue> setter, TValue to, double duration = 0.2f, Func<double, double>? easingFunction = null)
        where TTarget : IAnimatable
        where TValue : struct, ISubtractionOperators<TValue, TValue, TValue>, IMultiplyOperators<TValue, float, TValue>,
        IAdditionOperators<TValue, TValue, TValue>
    {
        var result = new AnimationSequence<TTarget>(target,
            new GenericTransitionAnimation<TValue>(getInitial, to, setter, duration, easingFunction));
        target.AddAnimation(result);
        return result;
    }

    public static AnimationSequence<TTarget> Transition<TTarget, TValue>(this AnimationSequence<TTarget> target,
        Func<TValue> getInitial, Action<TValue> setter, TValue to, double duration = 0.2f,
        Func<double, double>? easingFunction = null) where TTarget : IAnimatable
        where TValue : struct, ISubtractionOperators<TValue, TValue, TValue>, IMultiplyOperators<TValue, float, TValue>,
        IAdditionOperators<TValue, TValue, TValue>
    {
        return target.Add(new GenericTransitionAnimation<TValue>(getInitial, to, setter, duration, easingFunction));
    }

    public static AnimationSequence<TTarget> Transition<TTarget, TValue>(this TTarget target, TValue from, TValue to,
        Action<TValue> setter, double duration = 0.2f, Func<double, double>? easingFunction = null)
        where TTarget : IAnimatable
        where TValue : struct, ISubtractionOperators<TValue, TValue, TValue>, IMultiplyOperators<TValue, float, TValue>,
        IAdditionOperators<TValue, TValue, TValue>
    {
        var result = new AnimationSequence<TTarget>(target,
            new GenericTransitionAnimation<TValue>(() => from, to, setter, duration, easingFunction));
        target.AddAnimation(result);
        return result;
    }

    public static AnimationSequence<TTarget> Transition<TTarget, TValue>(this AnimationSequence<TTarget> target,
        TValue from, TValue to, Action<TValue> setter, double duration = 0.2f,
        Func<double, double>? easingFunction = null) where TTarget : IAnimatable
        where TValue : struct, ISubtractionOperators<TValue, TValue, TValue>, IMultiplyOperators<TValue, float, TValue>,
        IAdditionOperators<TValue, TValue, TValue>
    {
        return target.Add(new GenericTransitionAnimation<TValue>(() => from, to, setter, duration, easingFunction));
    }

    public static AnimationSequence<TTarget> Transition<TTarget>(this TTarget target, Func<Vector4> getInitial,
        Action<Vector4> setter, Vector4 to, double duration = 0.2f, Func<double, double>? easingFunction = null)
        where TTarget : IAnimatable
    {
        var result = new AnimationSequence<TTarget>(target,
            new Vector4TransitionAnimation(getInitial, to, setter, duration, easingFunction));
        target.AddAnimation(result);
        return result;
    }

    public static AnimationSequence<TTarget> Transition<TTarget>(this AnimationSequence<TTarget> target,
        Func<Vector4> getInitial,
        Action<Vector4> setter, Vector4 to, double duration = 0.2f, Func<double, double>? easingFunction = null)
        where TTarget : IAnimatable
    {
        return target.Add(new Vector4TransitionAnimation(getInitial, to, setter, duration, easingFunction));
    }

    public static AnimationSequence<TTarget> Transition<TTarget>(this TTarget target, Vector4 from, Vector4 to,
        Action<Vector4> setter, double duration = 0.2f, Func<double, double>? easingFunction = null)
        where TTarget : IAnimatable
    {
        var result = new AnimationSequence<TTarget>(target,
            new Vector4TransitionAnimation(() => from, to, setter, duration, easingFunction));
        target.AddAnimation(result);
        return result;
    }

    public static AnimationSequence<TTarget> Transition<TTarget>(this AnimationSequence<TTarget> target, Vector4 from,
        Vector4 to,
        Action<Vector4> setter, double duration = 0.2f, Func<double, double>? easingFunction = null)
        where TTarget : IAnimatable
    {
        return target.Add(new Vector4TransitionAnimation(() => from, to, setter, duration, easingFunction));
    }


    public static AnimationSequence<TTarget> Transition<TTarget>(this TTarget target, Func<Vector3> getInitial,
        Action<Vector3> setter, Vector3 to, double duration = 0.2f, Func<double, double>? easingFunction = null)
        where TTarget : IAnimatable
    {
        var result = new AnimationSequence<TTarget>(target,
            new Vector3TransitionAnimation(getInitial, to, setter, duration, easingFunction));
        target.AddAnimation(result);
        return result;
    }

    public static AnimationSequence<TTarget> Transition<TTarget>(this AnimationSequence<TTarget> target,
        Func<Vector3> getInitial,
        Action<Vector3> setter, Vector3 to, double duration = 0.2f, Func<double, double>? easingFunction = null)
        where TTarget : IAnimatable
    {
        return target.Add(new Vector3TransitionAnimation(getInitial, to, setter, duration, easingFunction));
    }

    public static AnimationSequence<TTarget> Transition<TTarget>(this TTarget target, Vector3 from, Vector3 to,
        Action<Vector3> setter, double duration = 0.2f, Func<double, double>? easingFunction = null)
        where TTarget : IAnimatable
    {
        var result = new AnimationSequence<TTarget>(target,
            new Vector3TransitionAnimation(() => from, to, setter, duration, easingFunction));
        target.AddAnimation(result);
        return result;
    }

    public static AnimationSequence<TTarget> Transition<TTarget>(this AnimationSequence<TTarget> target, Vector3 from,
        Vector3 to,
        Action<Vector3> setter, double duration = 0.2f, Func<double, double>? easingFunction = null)
        where TTarget : IAnimatable
    {
        return target.Add(new Vector3TransitionAnimation(() => from, to, setter, duration, easingFunction));
    }


    public static AnimationSequence<TTarget> Transition<TTarget>(this TTarget target, Func<Vector2> getInitial,
        Action<Vector2> setter, Vector2 to, double duration = 0.2f, Func<double, double>? easingFunction = null)
        where TTarget : IAnimatable
    {
        var result = new AnimationSequence<TTarget>(target,
            new Vector2TransitionAnimation(getInitial, to, setter, duration, easingFunction));
        target.AddAnimation(result);
        return result;
    }

    public static AnimationSequence<TTarget> Transition<TTarget>(this AnimationSequence<TTarget> target,
        Func<Vector2> getInitial,
        Action<Vector2> setter, Vector2 to, double duration = 0.2f, Func<double, double>? easingFunction = null)
        where TTarget : IAnimatable
    {
        return target.Add(new Vector2TransitionAnimation(getInitial, to, setter, duration, easingFunction));
    }

    public static AnimationSequence<TTarget> Transition<TTarget>(this TTarget target, Vector2 from, Vector2 to,
        Action<Vector2> setter, double duration = 0.2f, Func<double, double>? easingFunction = null)
        where TTarget : IAnimatable
    {
        var result = new AnimationSequence<TTarget>(target,
            new Vector2TransitionAnimation(() => from, to, setter, duration, easingFunction));
        target.AddAnimation(result);
        return result;
    }

    public static AnimationSequence<TTarget> Transition<TTarget>(this AnimationSequence<TTarget> target, Vector2 from,
        Vector2 to,
        Action<Vector2> setter, double duration = 0.2f, Func<double, double>? easingFunction = null)
        where TTarget : IAnimatable
    {
        return target.Add(new Vector2TransitionAnimation(() => from, to, setter, duration, easingFunction));
    }
}