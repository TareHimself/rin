using rin.Core.Animation;
using rin.Core.Math;
using rin.Widgets.Containers;

namespace rin.Widgets.Animation;

public static class WidgetAnimationExtensions
{
    public static AnimationSequence<T> RotateTo<T>(this T target,float to,double duration = 0.5f,float? from = null,Func<double,double>? easingFunction = null) where T : Widget
    {
        var result = new AnimationSequence<T>(target,[new SimpleTransitionAnimation<float>(() => from.GetValueOrDefault(target.Angle), to, (a) => target.Angle = a, duration,easingFunction)]);
        target.AnimationRunner.Add(result);
        return result;
    }
    
    public static AnimationSequence<T> RotateTo<T>(this AnimationSequence<T> target,float to,double duration = 0.5f,float? from = null,Func<double,double>? easingFunction = null) where T : Widget
    {
        target.Add(new SimpleTransitionAnimation<float>(() => from.GetValueOrDefault(target.Target.Angle), to, (a) => target.Target.Angle = a, duration,easingFunction));
        return target;
    }
    
    public static AnimationSequence<T> TranslateTo<T>(this T target,Vector2<float> to,double duration = 0.5f,Vector2<float>? from = null,Func<double,double>? easingFunction = null) where T : Widget
    {
        var anim = new SimpleTransitionAnimation<Vector2<float>>(() => from.GetValueOrDefault(target.Translate), to, (a) => target.Translate = a, duration,easingFunction);
        var result = new AnimationSequence<T>(target,[anim]);
        target.AnimationRunner.Add(result);
        return result;
    }
    
    public static AnimationSequence<T> TranslateTo<T>(this AnimationSequence<T> target,Vector2<float> to,double duration = 0.5f,Vector2<float>? from = null,Func<double,double>? easingFunction = null) where T : Widget
    {
        target.Add(new SimpleTransitionAnimation<Vector2<float>>(() => from.GetValueOrDefault(target.Target.Translate), to, (a) => target.Target.Translate = a, duration,easingFunction));
        return target;
    }
    
    public static AnimationSequence<T> ScaleTo<T>(this T target,Vector2<float> to,double duration = 0.5f,Vector2<float>? from = null,Func<double,double>? easingFunction = null) where T : Widget
    {
        var anim = new SimpleTransitionAnimation<Vector2<float>>(() => from.GetValueOrDefault(target.Scale), to, (a) => target.Scale = a, duration,easingFunction);
        var result = new AnimationSequence<T>(target,[anim]);
        target.AnimationRunner.Add(result);
        return result;
    }
    
    public static AnimationSequence<T> ScaleTo<T>(this AnimationSequence<T> target,Vector2<float> to,double duration = 0.5f,Vector2<float>? from = null,Func<double,double>? easingFunction = null) where T : Widget
    {
        target.Add(new SimpleTransitionAnimation<Vector2<float>>(() => from.GetValueOrDefault(target.Target.Scale), to, (a) => target.Target.Scale = a, duration,easingFunction));
        return target;
    }
    
    public static AnimationSequence<T> HeightTo<T>(this T target,float to,double duration = 0.5f,float? from = null,Func<double,double>? easingFunction = null) where T : Sizer
    {
        var anim = new SimpleTransitionAnimation<float>(() => from.GetValueOrDefault(target.HeightOverride.GetValueOrDefault(0)), to, (a) => target.HeightOverride = a, duration,easingFunction);
        var result = new AnimationSequence<T>(target,[anim]);
        target.AnimationRunner.Add(result);
        return result;
    }
    
    public static AnimationSequence<T> HeightTo<T>(this AnimationSequence<T> target,float to,double duration = 0.5f,float? from = null,Func<double,double>? easingFunction = null) where T : Sizer
    {
        target.Add(new SimpleTransitionAnimation<float>(() => from.GetValueOrDefault(target.Target.HeightOverride.GetValueOrDefault(0)), to, (a) => target.Target.HeightOverride = a, duration,easingFunction));
        return target;
    }
    
    public static AnimationSequence<T> WidthTo<T>(this T target,float to,double duration = 0.5f,float? from = null,Func<double,double>? easingFunction = null) where T : Sizer
    {
        var anim = new SimpleTransitionAnimation<float>(() => from.GetValueOrDefault(target.WidthOverride.GetValueOrDefault(0)), to, (a) => target.WidthOverride = a, duration,easingFunction);
        var result = new AnimationSequence<T>(target,[anim]);
        target.AnimationRunner.Add(result);
        return result;
    }
    
    public static AnimationSequence<T> WidthTo<T>(this AnimationSequence<T> target,float to,double duration = 0.5f,float? from = null,Func<double,double>? easingFunction = null) where T : Sizer
    {
        target.Add(new SimpleTransitionAnimation<float>(() => from.GetValueOrDefault(target.Target.WidthOverride.GetValueOrDefault(0)), to, (a) => target.Target.WidthOverride = a, duration,easingFunction));
        return target;
    }
}