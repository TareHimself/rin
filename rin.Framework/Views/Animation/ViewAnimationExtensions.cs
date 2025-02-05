using System.Numerics;
using rin.Framework.Core.Animation;
using rin.Framework.Core.Math;
using rin.Framework.Views.Composite;

namespace rin.Framework.Views.Animation;

public static class ViewAnimationExtensions
{
    public static AnimationSequence<T> RotateTo<T>(this T target, float to, double duration = 0.2f, float? from = null,
        Func<double, double>? easingFunction = null) where T : View => target.Transition(
        () => from.GetValueOrDefault(target.Angle), (a) => target.Angle = a, to, duration, easingFunction);

    public static AnimationSequence<T> RotateTo<T>(this AnimationSequence<T> target, float to, double duration = 0.2f,
        float? from = null, Func<double, double>? easingFunction = null) where T : View => target.Transition(
        () => from.GetValueOrDefault(target.Target.Angle), (a) => target.Target.Angle = a, to, duration,
        easingFunction);

    public static AnimationSequence<T> TranslateTo<T>(this T target, Vector2 to, double duration = 0.2f,
        Vector2? from = null, Func<double, double>? easingFunction = null) where T : View
        => target.Transition(() => from.GetValueOrDefault(target.Translate), (a) => target.Translate = a, to, duration,
            easingFunction);

    public static AnimationSequence<T> TranslateTo<T>(this AnimationSequence<T> target, Vector2 to,
        double duration = 0.2f, Vector2? from = null, Func<double, double>? easingFunction = null)
        where T : View
        => target.Transition(() => from.GetValueOrDefault(target.Target.Translate),
            (a) => target.Target.Translate = a, to, duration, easingFunction);

    public static AnimationSequence<T> PivotTo<T>(this T target, Vector2 to, double duration = 0.2f,
        Vector2? from = null, Func<double, double>? easingFunction = null) where T : View
        => target.Transition(() => from.GetValueOrDefault(target.Pivot), (a) => target.Pivot = a, to, duration,
            easingFunction);

    public static AnimationSequence<T> PivotTo<T>(this AnimationSequence<T> target, Vector2 to,
        double duration = 0.2f, Vector2? from = null, Func<double, double>? easingFunction = null)
        where T : View
        => target.Transition(() => from.GetValueOrDefault(target.Target.Pivot), (a) => target.Target.Pivot = a, to,
            duration, easingFunction);
    
    public static AnimationSequence<T> ScaleTo<T>(this T target, Vector2 to, double duration = 0.2f,
        Vector2? from = null, Func<double, double>? easingFunction = null) where T : View
        => target.Transition(() => from.GetValueOrDefault(target.Scale), (a) => target.Scale = a, to, duration,
            easingFunction);

    public static AnimationSequence<T> ScaleTo<T>(this AnimationSequence<T> target, Vector2 to,
        double duration = 0.2f, Vector2? from = null, Func<double, double>? easingFunction = null)
        where T : View
        => target.Transition(() => from.GetValueOrDefault(target.Target.Scale), (a) => target.Target.Scale = a, to,
            duration, easingFunction);

    public static AnimationSequence<T> HeightTo<T>(this T target, float to, double duration = 0.2f, float? from = null,
        Func<double, double>? easingFunction = null) where T : Sizer
        => target.Transition(() => from.GetValueOrDefault(target.HeightOverride.GetValueOrDefault(0)),
            (a) => target.HeightOverride = a, to, duration, easingFunction);

    public static AnimationSequence<T> HeightTo<T>(this AnimationSequence<T> target, float to, double duration = 0.2f,
        float? from = null, Func<double, double>? easingFunction = null) where T : Sizer
        => target.Transition(() => from.GetValueOrDefault(target.Target.HeightOverride.GetValueOrDefault(0)),
            (a) => target.Target.HeightOverride = a, to, duration, easingFunction);

    public static AnimationSequence<T> WidthTo<T>(this T target, float to, double duration = 0.2f, float? from = null,
        Func<double, double>? easingFunction = null) where T : Sizer
        => target.Transition(() => from.GetValueOrDefault(target.WidthOverride.GetValueOrDefault(0)),
            (a) => target.WidthOverride = a, to, duration, easingFunction);

    public static AnimationSequence<T> WidthTo<T>(this AnimationSequence<T> target, float to, double duration = 0.2f,
        float? from = null, Func<double, double>? easingFunction = null) where T : Sizer
        => target.Transition(() => from.GetValueOrDefault(target.Target.WidthOverride.GetValueOrDefault(0)),
            (a) => target.Target.WidthOverride = a, to, duration, easingFunction);
}