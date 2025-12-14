namespace Rin.Engine.World.Mesh.Skinning;

public interface IPoseSource
{
    public Skeleton Skeleton { get; }
    public Pose GetPose();
}