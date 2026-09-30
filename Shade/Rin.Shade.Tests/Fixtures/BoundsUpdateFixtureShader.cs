using System.Numerics;
using Rin.Shade;

namespace Rin.Shade.Tests.Fixtures;

// Port of Shaders/World/Mesh/Compute/bounds_update.slang - the real, working compute shader,
// picked because it needs no textures/bindless resources ([ShaderBinding] isn't built yet), while
// still exercising real constructor (__init) usage, a mutating struct method, and buffer indexing
// with both int and uint indices.

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

[ShaderStruct]
public struct Vertex
{
    public Vector4 LocationU;
    public Vector4 NormalV;
    public Vector4 Tangent;

    public Vector3 GetLocation() => LocationU.xyz;
}

public struct SkinnedMesh
{
    public int Index;
    public BufferRef<Vertex> Vertices;
    public uint Count;
}

public struct BoundsUpdatePushConstants
{
    public BufferRef<SkinnedMesh> SkinnedMeshes;
    public int TotalInvocations;
    public BufferRef<UpdatableBounds3D> Output;
}

[Shader("Fixtures/bounds_update.slang")]
public class BoundsUpdateFixtureShader : Shader
{
    [Push] protected BoundsUpdatePushConstants Push;

    [Compute(64, 1, 1)]
    public void Compute(ComputeIn input)
    {
        var index = input.ThreadId;
        if (index >= (uint)Push.TotalInvocations) return;

        var mesh = Push.SkinnedMeshes[index];
        var vertex = mesh.Vertices[0];
        var vertexCount = mesh.Count;
        var bounds = new UpdatableBounds3D(vertex.GetLocation());

        for (var i = 1u; i < vertexCount; i++)
        {
            vertex = mesh.Vertices[i];
            bounds.Update(vertex.GetLocation());
        }

        Push.Output[mesh.Index] = bounds;
    }
}
