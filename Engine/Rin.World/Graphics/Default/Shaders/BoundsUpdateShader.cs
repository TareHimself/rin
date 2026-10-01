using System.Numerics;
using JetBrains.Annotations;
using Rin.Shade;
using Rin.World.Graphics.Mesh;

namespace Rin.World.Graphics.Default.Shaders;

public static class VectorIntrinsics
{
    [SlangExpression("min(@0, @1)")] public static extern Vector3 Min(Vector3 a, Vector3 b);
    [SlangExpression("max(@0, @1)")] public static extern Vector3 Max(Vector3 a, Vector3 b);
}

// Fields are named Lower/Upper rather than Min/Max: lowered to Slang identifiers they'd collide
// with the builtin min(...)/max(...) functions called in Update below (SHADE0012).
[ShaderStruct]
public struct UpdatableBounds3D
{
    public Vector3 Lower;
    public Vector3 Upper;

    public UpdatableBounds3D(Vector3 location)
    {
        Lower = location;
        Upper = location;
    }

    public void Update(Vector3 location)
    {
        Lower = VectorIntrinsics.Min(Lower, location);
        Upper = VectorIntrinsics.Max(Upper, location);
    }
}

public struct ComputeIn
{
    [DispatchThreadId] public uint ThreadId;
}

[Shader("Shaders/Rin/World/Mesh/Compute/bounds_update.slang")]
public partial class BoundsUpdateShader : Shader
{
    public struct SkinnedMesh
    {
        public int Index;
        public BufferRef<Vertex> Vertices;
        public uint Count;
    }

    public struct PushConstants
    {
        public BufferRef<SkinnedMesh> SkinnedMeshes;
        public int TotalInvocations;
        public BufferRef<UpdatableBounds3D> Output;
    }

    [Push] [UsedImplicitly] protected PushConstants Push;

    [Compute(64, 1, 1)]
    public void Compute(ComputeIn input)
    {
        var index = input.ThreadId;
        if (index >= (uint)Push.TotalInvocations) return;

        var mesh = Push.SkinnedMeshes[index];
        var bounds = new UpdatableBounds3D(mesh.Vertices[0].Location);

        for (var i = 1u; i < mesh.Count; i++)
            bounds.Update(mesh.Vertices[i].Location);

        Push.Output[mesh.Index] = bounds;
    }
}
