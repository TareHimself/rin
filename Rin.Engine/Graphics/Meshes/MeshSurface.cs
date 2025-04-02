using Rin.Engine.Core;

namespace Rin.Engine.Graphics.Meshes;

public record struct MeshSurface
{
    public Bounds3D Bounds;
    public uint IndicesCount;
    public uint IndicesStart;
    public uint VertexCount;
    public uint VertexStart;
}