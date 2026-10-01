using JetBrains.Annotations;

namespace Rin.World.Graphics.Default;

/// <summary>
///     One vertex's skinning job: which pose, which source mesh and which vertex in it. Written by the
///     CPU (SkinningPass) and read on the GPU by SkinningShader, so the layout lives in one place.
/// </summary>
[NoReorder]
public struct SkinningExecutionInfo
{
    public required int PoseIndex;
    public required int MeshIndex;
    public required int VertexIndex;
}
