namespace aerox.Runtime.Widgets.Animation;

public abstract class AnimationAction
{
    
    public virtual void OnStart(AnimationState state)
    {
        
    }

    public virtual bool Apply(AnimationState state,float current)
    {
        if (state.Active)
        {
            if (!ApplyAction(state,current))
            {
                OnEnd(state);
                return false;
            }
            
            return true;
        }

        return false;
    }

    public virtual AnimationState NewState(Widget widget) =>
        new AnimationState((float)SRuntime.Get().GetElapsedRuntimeTimeSeconds(), this, widget);
    
    /// <summary>
    /// Applies the actual animation, return true to keep animating and false to stop animating
    /// </summary>
    /// <param name="state"></param>
    /// <param name="start"></param>
    /// <param name="current"></param>
    /// <returns></returns>
    protected abstract bool ApplyAction(AnimationState state,float current);

    protected virtual void OnEnd(AnimationState state)
    {
        if (state.Active)
        {
            state.OnEnd();
        }
    }
}