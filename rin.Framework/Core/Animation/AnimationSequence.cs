namespace rin.Framework.Core.Animation;

public class AnimationSequence<T> : IAnimation where T : IAnimatable
{
    private readonly HashSet<AnimationState> _animations;
    private double _startTime;

    public AnimationSequence(T target, params IAnimation[] animations)
    {
        Target = target;
        _animations = animations.Select(c => new AnimationState
        {
            StartTime = _startTime,
            Animation = c
        }).ToHashSet();
        Duration = _animations.MaxBy(c => c.Duration)?.Duration ?? 0.0;
    }

    public T Target { get; private set; }
    public double Duration { get; private set; }

    public void Start(double elapsed)
    {
        Update(elapsed);
    }

    public void Update(double elapsed)
    {
        _animations.RemoveWhere(c => !(c.StartTime > elapsed) && c.Update(elapsed));
    }

    /// <summary>
    ///     Run the next <see cref="IAnimation" />'s after all the previous ones
    /// </summary>
    /// <returns></returns>
    public AnimationSequence<T> After()
    {
        _startTime = Duration;
        return this;
    }

    public AnimationSequence<T> Add(IAnimation animation)
    {
        var newAnim = new AnimationState
        {
            StartTime = _startTime,
            Animation = animation
        };
        _animations.Add(newAnim);
        Duration = System.Math.Max(Duration, newAnim.StartTime + newAnim.Duration);
        return this;
    }
}