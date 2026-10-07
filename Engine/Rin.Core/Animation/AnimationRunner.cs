namespace Rin.Core.Animation;

public class AnimationRunner(IApplication? application = null)
{
    private readonly HashSet<AnimationState> _animations = [];
    private readonly IApplication _application = application ?? IApplication.Get();
    private readonly List<AnimationState> _scratch = [];
    private float _currentEndTime;

    public void Update()
    {
        AnimationState.UpdateAll(_animations, _scratch, _application.TimeSeconds);
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