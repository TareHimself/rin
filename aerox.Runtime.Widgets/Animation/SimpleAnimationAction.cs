namespace aerox.Runtime.Widgets.Animation;

public class SimpleAnimationAction(Func<AnimationState,float,bool> apply) : AnimationAction
{
    protected override bool ApplyAction(AnimationState state,float current) => apply(state,current);
}