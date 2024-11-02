using rin.Core;

namespace rin.Widgets.Animation;

public class AnimationState(float startTime, AnimationAction animation, Widget target)
{
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    public float StartTime = startTime;
    public readonly AnimationAction Animation = animation;
    public readonly Widget Target = target;
    public bool Active { get; private set; }

    public void Start()
    {
        Active = true;
        StartTime = (float)SRuntime.Get().GetTimeSeconds();
        Animation.OnStart(this);
    }

    public virtual bool Apply(float currentTime) => Animation.Apply(this,currentTime);

    public void OnEnd()
    {
        Active = false;
    }
}