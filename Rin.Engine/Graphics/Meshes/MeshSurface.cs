using System.Numerics;

namespace Rin.Engine.Graphics.Meshes;

public record struct MeshSurface
{
    public uint VertexIndex;
    public uint VertexCount;
    public uint IndicesCount;
}