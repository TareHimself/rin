namespace Rin.World.Mesh.Skinning.Animation;

public class AnimationGraph(Skeleton skeleton) : IPoseSource
{
    private SkeletalPose _cachedPose = skeleton.BasePose;
    private readonly List<IAnimationNotify> _firedNotifies = [];
    private readonly HashSet<AnimationNotifyStateRange> _activeThisTick = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<AnimationNotifyStateRange, IAnimationNotifyState> _activeStates =
        new(ReferenceEqualityComparer.Instance);
    private readonly List<IAnimationNotifyState> _begunStates = [];
    private readonly List<IAnimationNotifyState> _endedStates = [];
    private List<AnimationNotifyStateRange>? _endedRangesScratch;

    public Skeleton Skeleton => skeleton;
    public IPoseNode? Root { get; set; }

    public IReadOnlyList<IAnimationNotify> FiredNotifies => _firedNotifies;
    public IReadOnlyList<IAnimationNotifyState> BegunNotifyStates => _begunStates;
    public IReadOnlyList<IAnimationNotifyState> EndedNotifyStates => _endedStates;

    public void Tick(float deltaSeconds)
    {
        _firedNotifies.Clear();
        _begunStates.Clear();
        _endedStates.Clear();
        _activeThisTick.Clear();

        if (Root is not null)
        {
            var ctx = new AnimationEvalContext(deltaSeconds, _firedNotifies, _activeThisTick);
            using var pose = Root.Evaluate(ctx);
            _cachedPose = pose.ToSkeletalPose();
        }

        DiffNotifyStates();
    }

    private void DiffNotifyStates()
    {
        foreach (var range in _activeThisTick)
            if (!_activeStates.ContainsKey(range))
            {
                var instance = range.Factory.Create();
                _activeStates[range] = instance;
                _begunStates.Add(instance);
            }

        foreach (var (range, instance) in _activeStates)
            if (!_activeThisTick.Contains(range))
            {
                _endedStates.Add(instance);
                range.Factory.Release(instance);
                (_endedRangesScratch ??= []).Add(range);
            }

        if (_endedRangesScratch is not { } ended) return;
        foreach (var range in ended) _activeStates.Remove(range);
        ended.Clear();
    }

    public SkeletalPose GetPose()
    {
        return _cachedPose;
    }
}
