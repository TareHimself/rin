namespace Rin.World.Mesh.Skinning.Animation;

public class AnimationGraph(Skeleton skeleton) : IPoseSource
{
    private SkeletalPose _cachedPose = skeleton.BasePose;
    private readonly NotifySink _notifySink = new();
    private readonly List<AnimationNotify> _firedDefinitions = [];
    private readonly List<IAnimationNotify> _firedNotifies = [];
    private readonly HashSet<AnimationNotifyStateRange> _activeThisTick = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<AnimationNotifyStateRange, IAnimationNotifyState> _activeStates =
        new(ReferenceEqualityComparer.Instance);
    private readonly List<IAnimationNotifyState> _begunStates = [];
    private readonly List<AnimationNotifyStateRange> _endedRanges = [];
    private readonly List<IAnimationNotifyState> _endedStates = [];

    public Skeleton Skeleton => skeleton;
    public IPoseNode? Root { get; set; }

    public IReadOnlyList<IAnimationNotify> FiredNotifies => _firedNotifies;
    public IReadOnlyList<IAnimationNotifyState> BegunNotifyStates => _begunStates;
    public IReadOnlyList<IAnimationNotifyState> EndedNotifyStates => _endedStates;

    public void Tick(float deltaSeconds)
    {
        _notifySink.Clear();
        _firedDefinitions.Clear();
        _firedNotifies.Clear();
        _begunStates.Clear();
        _endedRanges.Clear();
        _endedStates.Clear();
        _activeThisTick.Clear();

        if (Root is not null)
        {
            var ctx = new AnimationEvalContext(deltaSeconds, _notifySink, _activeThisTick);
            using var pose = Root.Evaluate(ctx);
            _cachedPose = pose.ToSkeletalPose();
        }

        _notifySink.CollectFired(_firedDefinitions);
        foreach (var notify in _firedDefinitions) _firedNotifies.Add(notify.Factory.Create());
        DiffNotifyStates();
    }

    public void ReleaseNotifies()
    {
        for (var i = 0; i < _firedNotifies.Count; i++) _firedDefinitions[i].Factory.Release(_firedNotifies[i]);
        for (var i = 0; i < _endedStates.Count; i++) _endedRanges[i].Factory.Release(_endedStates[i]);

        _notifySink.Clear();
        _firedDefinitions.Clear();
        _firedNotifies.Clear();
        _begunStates.Clear();
        _endedRanges.Clear();
        _endedStates.Clear();
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
                _endedRanges.Add(range);
                _endedStates.Add(instance);
            }

        foreach (var range in _endedRanges) _activeStates.Remove(range);
    }

    public SkeletalPose GetPose()
    {
        return _cachedPose;
    }
}
