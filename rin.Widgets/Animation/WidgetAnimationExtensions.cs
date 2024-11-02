using rin.Core.Animation;
using rin.Core.Math;

namespace rin.Widgets.Animation;

public static class WidgetAnimationExtensions
{
    public static ParallelAnimation<T> RotateTo<T>(this T target, float? from,float to,double duration = 3.0f) where T : Widget
    {
        var result = new ParallelAnimation<T>(target,[new RotateToAnimation<T>(target,from.GetValueOrDefault(target.Angle), to,duration)]);
        target.AnimationRunner.Add(result);
        return result;
    }
    
    public static ParallelAnimation<T> RotateTo<T>(this ParallelAnimation<T> target, float? from,float to,double duration = 3.0f) where T : Widget
    {
        target.Add(new RotateToAnimation<T>(target.Target,from.GetValueOrDefault(target.Target.Angle), to,duration));
        return target;
    }
    
    public static ParallelAnimation<T> TranslateTo<T>(this T target, Vector2<float>? from,Vector2<float> to,double duration = 3.0f,Func<double,double>? easingFunction = null) where T : Widget
    {
        var anim = new SimpleTransitionAnimation<Vector2<float>>(from.GetValueOrDefault(target.Translate), to, (a) => target.Translate = a, duration,easingFunction);
        var result = new ParallelAnimation<T>(target,[anim]);
        target.AnimationRunner.Add(result);
        return result;
    }
    
    public static ParallelAnimation<T> TranslateTo<T>(this ParallelAnimation<T> target, Vector2<float>? from,Vector2<float> to,double duration = 3.0f,Func<double,double>? easingFunction = null) where T : Widget
    {
        target.Add(new SimpleTransitionAnimation<Vector2<float>>(from.GetValueOrDefault(target.Target.Translate), to, (a) => target.Target.Translate = a, duration,easingFunction));
        return target;
    }
    
    public static ParallelAnimation<T> ScaleTo<T>(this T target, Vector2<float>? from,Vector2<float> to,double duration = 3.0f,Func<double,double>? easingFunction = null) where T : Widget
    {
        var anim = new SimpleTransitionAnimation<Vector2<float>>(from.GetValueOrDefault(target.Scale), to, (a) => target.Scale = a, duration,easingFunction);
        var result = new ParallelAnimation<T>(target,[anim]);
        target.AnimationRunner.Add(result);
        return result;
    }
    
    public static ParallelAnimation<T> ScaleTo<T>(this ParallelAnimation<T> target, Vector2<float>? from,Vector2<float> to,double duration = 3.0f,Func<double,double>? easingFunction = null) where T : Widget
    {
        target.Add(new SimpleTransitionAnimation<Vector2<float>>(from.GetValueOrDefault(target.Target.Scale), to, (a) => target.Target.Scale = a, duration,easingFunction));
        return target;
    }
}