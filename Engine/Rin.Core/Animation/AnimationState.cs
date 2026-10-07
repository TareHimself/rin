namespace Rin.Core.Animation;

public class AnimationState
{
    public bool Active;
    public required IAnimation Animation;
    public required float StartTime;
    public float Duration => Animation.Duration;

    internal static void UpdateAll(HashSet<AnimationState> animations, List<AnimationState> scratch, float elapsed)
    {
        if (animations.Count == 0) return;

        scratch.AddRange(animations);
        foreach (var state in scratch)
            if (!(state.StartTime > elapsed) && state.Update(elapsed))
                animations.Remove(state);
        scratch.Clear();
    }

    public bool Update(float elapsed)
    {
        var animElapsed = elapsed - StartTime;
        if (!Active)
        {
            Active = true;
            Animation.Start(animElapsed);
        }

        Animation.Update(float.Min(animElapsed, Duration));
        return animElapsed >= Duration;
    }
}
