namespace Rin.World.Mesh.Skinning.Animation;

public readonly record struct AnimationEvalContext(
    float DeltaSeconds,
    NotifySink? Notifies = null,
    HashSet<AnimationNotifyStateRange>? ActiveNotifyStates = null,
    float Weight = 1f)
{
    public AnimationEvalContext Scaled(float factor)
    {
        return this with { Weight = Weight * factor };
    }
}
