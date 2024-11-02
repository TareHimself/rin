namespace rin.Core.Animation;

public static class AnimationExtensions
{
    
    public static ParallelAnimation<T> Then<T>(this ParallelAnimation<T> target)
        where T : IAnimatable
    {
        var next = new ParallelAnimation<T>(target.Target,[]);
        target.Next = next;
        return next;
    }
    
    public static ParallelAnimation<T> Test<T>(this T target, double duration, string id)
        where T : IAnimatable
    {
        var result = new ParallelAnimation<T>(target,[new TestAnimation(duration, id)]);
        target.AnimationRunner.Add(result);
        return result;
    }
    
    public static ParallelAnimation<T> Test<T>(this ParallelAnimation<T> target, double duration, string id)
        where T : IAnimatable
    {
        target.Add(new TestAnimation(duration, id));
        return target;
    }

    public static T StopAll<T>(this T target) where T : IAnimatable
    {
        target.AnimationRunner.StopAll();
        return target;
    }
}