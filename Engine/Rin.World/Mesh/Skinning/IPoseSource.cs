namespace Rin.World.Mesh.Skinning;

public interface IPoseSource
{
    public Skeleton Skeleton { get; }
    public SkeletalPose GetPose();

    public void Tick(float deltaSeconds)
    {
    }
}