namespace Rin.World.Mesh.Skinning.Animation;

public class BlendNode(IPoseNode a, IPoseNode b) : IPoseNode
{
    public float Weight;

    public PooledSkeletalPose Evaluate(in AnimationEvalContext ctx)
    {
        using var poseA = a.Evaluate(ctx);
        using var poseB = b.Evaluate(ctx);
        return PooledSkeletalPose.Blend(poseA, poseB, Weight);
    }
}
