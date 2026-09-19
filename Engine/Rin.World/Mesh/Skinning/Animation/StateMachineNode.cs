namespace Rin.World.Mesh.Skinning.Animation;

public class StateMachineNode : IPoseNode
{
    private readonly IReadOnlyDictionary<string, AnimationState> _states;
    private AnimationState _current;
    private AnimationTransition? _activeTransition;
    private float _transitionElapsed;

    public StateMachineNode(IReadOnlyDictionary<string, AnimationState> states, string initialState)
    {
        _states = states;
        _current = states[initialState];
    }

    public string CurrentStateName => _current.Name;

    public PooledSkeletalPose Evaluate(in AnimationEvalContext ctx)
    {
        if (_activeTransition is null)
            foreach (var transition in _current.Transitions)
                if (transition.Condition())
                {
                    _activeTransition = transition;
                    _transitionElapsed = 0f;
                    break;
                }

        if (_activeTransition is null) return _current.Node.Evaluate(ctx);

        _transitionElapsed += ctx.DeltaSeconds;
        var target = _states[_activeTransition.ToState];
        var alpha = _activeTransition.BlendDuration <= 0f
            ? 1f
            : float.Min(_transitionElapsed / _activeTransition.BlendDuration, 1f);

        using var fromPose = _current.Node.Evaluate(ctx);
        using var toPose = target.Node.Evaluate(ctx);
        var blended = PooledSkeletalPose.Blend(fromPose, toPose, alpha);

        if (alpha >= 1f)
        {
            _current = target;
            _activeTransition = null;
        }

        return blended;
    }
}
