using JetBrains.Annotations;

namespace Rin.World.Graphics.Default;

/// <summary>
///     One mesh's draw parameters as the indirect-fill shader reads them. Written by the CPU
///     (SceneDataPass) and read on the GPU by DrawIndirectShader, so the layout lives in one place.
/// </summary>
[NoReorder]
public struct IndirectMeshRecord
{
    public required uint IndicesCount;
    public required uint IndicesStart;
    public required uint VertexStart;
    public required uint Instance;
    public required int MeshIndex;
}
