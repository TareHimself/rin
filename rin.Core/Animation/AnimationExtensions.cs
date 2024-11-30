using System.Numerics;
using JetBrains.Annotations;

namespace rin.Core.Animation;

public static class AnimationExtensions
{
    /// <summary>
    /// Stops all animations running on this <see cref="IAnimatable"/>
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
    /// Creates a new <see cref="AnimationSequence{T}"/>
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
    /// Will run the action as soon as possible
    /// </summary>
    /// <param name="self"></param>
    /// <param name="action"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static AnimationSequence<T> Do<T>(this T self, Action action) where T : IAnimatable =>
        self.Sequence().Do(action);

    /// <summary>
    /// Will run the action as soon as possible
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
    /// Delay for the specified duration, will add an <see cref="AnimationSequence{T}.After"/> before and after the delay
    /// </summary>
    /// <param name="self"></param>
    /// <param name="duration"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static AnimationSequence<T> Delay<T>(this T self, double duration)
        where T : IAnimatable => self.Sequence().Add(new DelayAnimation(duration)).After();

    /// <summary>
    /// Delay for the specified duration, will add an <see cref="AnimationSequence{T}.After"/> before and after the delay
    /// </summary>
    /// <param name="self"></param>
    /// <param name="duration"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static AnimationSequence<T> Delay<T>(this AnimationSequence<T> self, double duration)
        where T : IAnimatable => self.After().Add(new DelayAnimation(duration)).After();


    /// <summary>
    /// Runs the <see cref="AnimationSequence{T}"/> after all the previous <see cref="IAnimation"/>'s have completed
    /// </summary>
    /// <param name="self"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static AnimationSequence<T> After<T>(this T self) where T : IAnimatable => self.AnimationRunner.After(self);

    public static AnimationSequence<TTarget> Transition<TTarget, TValue>(this TTarget target, Func<TValue> getInitial,
        Action<TValue> setter, TValue to, double duration = 0.2f, Func<double, double>? easingFunction = null)
        where TTarget : IAnimatable
        where TValue : struct, ISubtractionOperators<TValue, TValue, TValue>, IMultiplyOperators<TValue, float, TValue>,
        IAdditionOperators<TValue, TValue, TValue>
    {
        var result = new AnimationSequence<TTarget>(target,
            new TransitionAnimation<TValue>(getInitial, to, setter, duration, easingFunction));
        target.AddAnimation(result);
        return result;
    }

    public static AnimationSequence<TTarget> Transition<TTarget, TValue>(this AnimationSequence<TTarget> target,
        Func<TValue> getInitial, Action<TValue> setter, TValue to, double duration = 0.2f,
        Func<double, double>? easingFunction = null) where TTarget : IAnimatable
        where TValue : struct, ISubtractionOperators<TValue, TValue, TValue>, IMultiplyOperators<TValue, float, TValue>,
        IAdditionOperators<TValue, TValue, TValue>
        => target.Add(new TransitionAnimation<TValue>(getInitial, to, setter, duration, easingFunction));

    public static AnimationSequence<TTarget> Transition<TTarget, TValue>(this TTarget target, TValue from, TValue to,
        Action<TValue> setter, double duration = 0.2f, Func<double, double>? easingFunction = null)
        where TTarget : IAnimatable
        where TValue : struct, ISubtractionOperators<TValue, TValue, TValue>, IMultiplyOperators<TValue, float, TValue>,
        IAdditionOperators<TValue, TValue, TValue>
    {
        var result = new AnimationSequence<TTarget>(target,
            new TransitionAnimation<TValue>(() => from, to, setter, duration, easingFunction));
        target.AddAnimation(result);
        return result;
    }

    public static AnimationSequence<TTarget> Transition<TTarget, TValue>(this AnimationSequence<TTarget> target,
        TValue from, TValue to, Action<TValue> setter, double duration = 0.2f,
        Func<double, double>? easingFunction = null) where TTarget : IAnimatable
        where TValue : struct, ISubtractionOperators<TValue, TValue, TValue>, IMultiplyOperators<TValue, float, TValue>,
        IAdditionOperators<TValue, TValue, TValue>
    => target.Add(new TransitionAnimation<TValue>(() => from, to, setter, duration, easingFunction));
    
}