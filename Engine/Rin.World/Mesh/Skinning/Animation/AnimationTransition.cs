namespace Rin.World.Mesh.Skinning.Animation;

public class AnimationTransition
{
    public required string ToState;
    public required Func<bool> Condition;
    public float BlendDuration = 0.25f;
}
