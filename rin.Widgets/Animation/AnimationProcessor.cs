using System.Collections.Concurrent;
using rin.Core;

namespace rin.Widgets.Animation;

public class AnimationProcessor
{
    public readonly ConcurrentDictionary<string, AnimationState> Actions = [];

    public string Run(Widget target,AnimationAction animation)
    {
        var animationState = animation.NewState(target);
        
        Actions.TryAdd(animationState.Id,animationState);
        animationState.Start();
        return animationState.Id;
    }


    public void Apply()
    {
        var currentTime = (float)SRuntime.Get().GetTimeSeconds();
        List<string> idsToRemove = [];
        foreach (var (_,state) in Actions)
        {
            if (state.Active)
            {
                state.Apply(currentTime);
                if(!state.Active) idsToRemove.Add(state.Id);
            }
        }
        
        foreach (var id in idsToRemove)
        {
            Actions.Remove(id, out var _);
        }
    }
}