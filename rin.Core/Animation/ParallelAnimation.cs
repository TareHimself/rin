namespace rin.Core.Animation;

public class ParallelAnimation<T> : IAnimation where T : IAnimatable
{
    public double Duration => _animations.MaxBy(c => c.TotalDuration)?.TotalDuration ?? 0.0f;


    private readonly List<ActiveAnimation> _animations;
    private readonly HashSet<int> _activeAnimations;
    public T Target { get; private set; }

    public ParallelAnimation(T target,IEnumerable<IAnimation> animations)
    {
        Target = target;
        _animations = animations.Select(c => new ActiveAnimation(c)).ToList();
        _activeAnimations = _animations.Select((c, i) => i).ToHashSet();
    }

    public void DoUpdate(double start, double current)
    {
        _activeAnimations.RemoveWhere(c => _animations[c].Update(start, current));
    }

    

    public ParallelAnimation<T> Add(IAnimation animation)
    {
        _activeAnimations.Add(_animations.Count);
        _animations.Add(new ActiveAnimation(animation));
        
        return this;
    }
    
    public IAnimation? Next { get; set; }
}