namespace Rin.Engine.World.Skinning;

public class Skeleton
{

    public Bone Root;
    public readonly Dictionary<string,Bone> Bones;
    public readonly VertexBoneInfo[] BoneInfos;

    public Skeleton(Bone[] bones, VertexBoneInfo[] boneInfos) : this(
        bones.First(c => c.Parent == null) ?? throw new NullReferenceException(),
        bones.ToDictionary(b => b.Name),
        boneInfos)
    {
    }
    
    public Skeleton(Bone root,Dictionary<string,Bone> bones, VertexBoneInfo[] boneInfos)
    {
        Root = root;
        Bones = bones;
        BoneInfos = boneInfos;
    }
}