using JetBrains.Annotations;
using Rin.Core.Shared;

namespace Rin.Core.Graphics.Meshes;

[NoReorder]
public record struct MeshSurface
{
    public Bounds3D Bounds;
    public uint IndicesCount;
    public uint IndicesStart;
    public uint VertexCount;
    public uint VertexStart;
}