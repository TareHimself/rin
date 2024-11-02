namespace rin.Core.Animation;

public class SequenceAnimation<T> : IAnimation where T : IAnimatable
{
    public double Duration => _animations.Aggregate(0.0,(c,t) => c + t.TotalDuration);

    public int CurrentAnimationIndex;


    private readonly List<IAnimation> _animations;
    private double _accumulatedDuration;

    public ActiveAnimation? CurrentActiveAnimation;

    public T Target { get; private set; }
    
    public SequenceAnimation(T target,IEnumerable<IAnimation> animations)
    {
        Target = target;
        _animations = animations.ToList();
    }

    public void DoUpdate(double start, double current)
    {
        if (CurrentActiveAnimation == null && CurrentAnimationIndex < _animations.Count)
        {
            CurrentActiveAnimation = new ActiveAnimation(_animations[CurrentAnimationIndex]);
        }

        if (CurrentActiveAnimation is not { } asCurrentActiveAnimation) return;
        
        while (true)
        {
            if (asCurrentActiveAnimation.Update(start + _accumulatedDuration, current))
            {
                _accumulatedDuration += asCurrentActiveAnimation.TotalDuration;
                CurrentAnimationIndex++;
                CurrentActiveAnimation = null;
                continue;
            }
            break;
        }

    }

    public IAnimation? Next { get; set; }
    

    public SequenceAnimation<T> Add(IAnimation animation)
    {
        _animations.Add(animation);
        return this;
    }

    
}