namespace Rin.Framework.Animation;

public class AnimationRunner
{
    private readonly HashSet<AnimationState> _animations = [];
    private float _currentEndTime;

    public void Update()
    {
        var elapsed = SApplication.Get().GetTimeSeconds();

        _animations.RemoveWhere(c => !(c.StartTime > elapsed) && c.Update(elapsed));
    }

    public IAnimation Add(AnimationState animation)
    {
        _animations.Add(animation);
        _currentEndTime = float.Max(_currentEndTime, animation.StartTime + animation.Duration);
        return animation.Animation;
    }

    public IAnimation Add(IAnimation animation)
    {
        return Add(new AnimationState
        {
            Animation = animation,
            StartTime = SApplication.Get().GetTimeSeconds()
        });
    }

    public AnimationSequence<T> After<T>(T target) where T : IAnimatable
    {
        var sequence = new AnimationSequence<T>(target);
        Add(new AnimationState
        {
            Animation = sequence,
            StartTime = SApplication.Get().GetTimeSeconds()
        });
        return sequence;
    }

    public void StopAll()
    {
        _animations.Clear();
    }
}