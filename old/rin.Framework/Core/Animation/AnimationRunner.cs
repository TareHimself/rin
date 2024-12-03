namespace rin.Framework.Core.Animation;

public class AnimationRunner
{
    private readonly HashSet<AnimationState> _animations = [];
    private double _currentEndTime = 0.0f;
    public void Update()
    {
        var elapsed = SRuntime.Get().GetTimeSeconds();

        _animations.RemoveWhere(c => !(c.StartTime > elapsed) && c.Update(elapsed));
    }
    
    public IAnimation Add(AnimationState animation)
    {
        _animations.Add(animation);
        _currentEndTime = System.Math.Max(_currentEndTime, animation.StartTime + animation.Duration);
        return animation.Animation;
    }
    public IAnimation Add(IAnimation animation) => Add(new AnimationState
    {
        Animation = animation,
        StartTime = SRuntime.Get().GetTimeSeconds()
    });
    
    public AnimationSequence<T> After<T>(T target) where T : IAnimatable
    {
        var sequence = new AnimationSequence<T>(target);
        Add(new AnimationState
        {
            Animation = sequence,
            StartTime = SRuntime.Get().GetTimeSeconds()
        });
        return sequence;
    }

    public void StopAll()
    {
        _animations.Clear();
    }
}