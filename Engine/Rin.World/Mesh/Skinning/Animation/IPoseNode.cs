namespace Rin.World.Mesh.Skinning.Animation;

public interface IPoseNode
{
    PooledSkeletalPose Evaluate(in AnimationEvalContext ctx);
}
