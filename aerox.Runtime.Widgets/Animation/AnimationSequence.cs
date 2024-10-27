namespace aerox.Runtime.Widgets.Animation;

public class AnimationSequence(params AnimationAction[] animations) : AnimationAction
{
    public class State(float startTime, AnimationAction animation, Widget target, Queue<AnimationAction> pendingActions) : AnimationState(startTime, animation, target)
    {
        public AnimationState? ActiveState;
        public Queue<AnimationAction> PendingActions = pendingActions;
    }
    
    private readonly AnimationAction[] _animations = animations;

    public override void OnStart(AnimationState target)
    {
        base.OnStart(target);
        _animations.FirstOrDefault()?.OnStart(target);
    }

    public override AnimationState NewState(Widget widget) => new State((float)SRuntime.Get().GetElapsedRuntimeTimeSeconds(),
        this, widget, _animations.Aggregate(new Queue<AnimationAction>(),
            (t, c) =>
            {
                t.Enqueue(c);
                return t;
            }));

    protected override bool ApplyAction(AnimationState state,float current)
    {
        var myState = (State)state;
        
        if (myState.PendingActions.Count == 0) return true;
        
        if (myState.ActiveState == null)
        {
            do
            {
                if (myState.ActiveState != null)
                {
                    myState.PendingActions.Dequeue();
                    myState.ActiveState = null;
                }
                
                if(myState.PendingActions.Count == 0) break;
                
                var anim = myState.PendingActions.Peek();
                myState.ActiveState = anim.NewState(state.Target);
                
            } while (!myState.ActiveState.Apply(current));

            if (myState.PendingActions.Count == 0)
            {
                return false;
            }

            return true;
        }
        else
        {
            if (myState.ActiveState?.Apply(current) == false)
            {
                myState.ActiveState = null;
                return Apply(state, current);
            }
        }
        
        return true;
    }
}