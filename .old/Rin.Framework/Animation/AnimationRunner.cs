namespace Rin.Framework.Animation;

public class AnimationRunner
{
    private readonly HashSet<AnimationState> _animations = [];
    private float _currentEndTime;
    private readonly IApplication _application = IApplication.Get();
    public void Update()
    {
        var elapsed = _application.TimeSeconds;

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
            StartTime = _application.TimeSeconds
        });
    }

    public AnimationSequence<T> After<T>(T target) where T : IAnimatable
    {
        var sequence = new AnimationSequence<T>(target);
        Add(new AnimationState
        {
            Animation = sequence,
            StartTime = _application.TimeSeconds
        });
        return sequence;
    }

    public void StopAll()
    {
        _animations.Clear();
    }
}