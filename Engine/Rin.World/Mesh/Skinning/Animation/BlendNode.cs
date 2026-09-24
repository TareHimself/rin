namespace Rin.World.Mesh.Skinning.Animation;

public class BlendNode(IPoseNode a, IPoseNode b) : IPoseNode
{
    public float Weight;

    public PooledSkeletalPose Evaluate(in AnimationEvalContext ctx)
    {
        using var poseA = a.Evaluate(ctx.Scaled(1f - Weight));
        using var poseB = b.Evaluate(ctx.Scaled(Weight));
        return PooledSkeletalPose.Blend(poseA, poseB, Weight);
    }
}
