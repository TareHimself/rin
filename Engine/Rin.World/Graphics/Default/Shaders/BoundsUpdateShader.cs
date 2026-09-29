using System.Numerics;
using Rin.Shade;

namespace Rin.World.Graphics.Default.Shaders;

// Rin.Shade-authored port of Shaders/World/Mesh/Compute/bounds_update.slang - the first real shader
// to run through the Rin.Shade.MSBuild pipeline rather than a test fixture. The [Shader] path lives
// under Shaders/Rin/World/, a scheme no hand-written shader's RinSlangDiscoverPrefix claims - never
// written to disk, compiled and embedded directly by Rin.Shade.MSBuild, so there's no collision with
// the still-untouched hand-written bounds_update.slang or BoundsUpdatePass.cs's real reference to it.

public static class VectorIntrinsics
{
    [SlangCall("min($0, $1)")] public static extern Vector3 Min(Vector3 a, Vector3 b);
    [SlangCall("max($0, $1)")] public static extern Vector3 Max(Vector3 a, Vector3 b);
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

// "Shade"-prefixed: these are GPU-side mirror types for transpilation only, distinct from
// Rin.World's real CPU-side Vertex (Rin.World.Graphics.Mesh)/SkinnedMesh (Rin.World.Mesh.Skinning)
// domain types, which happen to share these names in other namespaces in this same project.
[ShaderStruct]
public struct ShadeVertex
{
    public Vector4 LocationU;
    public Vector4 NormalV;
    public Vector4 Tangent;

    public Vector3 GetLocation() => LocationU.xyz;
}

public struct ShadeSkinnedMesh
{
    public int Index;
    public BufferRef<ShadeVertex> Vertices;
    public uint Count;
}

public struct BoundsUpdatePushConstants
{
    public BufferRef<ShadeSkinnedMesh> SkinnedMeshes;
    public int TotalInvocations;
    public BufferRef<UpdatableBounds3D> Output;
}

public struct ComputeIn
{
    [Semantic("SV_DispatchThreadID")] public uint ThreadId;
}

[Shader("Shaders/Rin/World/Mesh/Compute/bounds_update.slang")]
public partial class BoundsUpdateShader : Shader
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
