namespace Rin.World.Mesh.Skinning.Animation;

public readonly record struct AnimationEvalContext(
    float DeltaSeconds,
    List<IAnimationNotify>? FiredNotifies = null,
    HashSet<AnimationNotifyStateRange>? ActiveNotifyStates = null);
