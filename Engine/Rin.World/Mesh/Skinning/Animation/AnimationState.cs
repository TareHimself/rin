namespace Rin.World.Mesh.Skinning.Animation;

public class AnimationState
{
    public required string Name;
    public required IPoseNode Node;
    public List<AnimationTransition> Transitions = [];
}
