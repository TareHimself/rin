using System.Numerics;
using Rin.Engine.Core;

namespace Rin.Engine.Graphics.Meshes;

public record struct MeshSurface
{
    public uint VertexStart;
    public uint IndicesStart;
    public uint VertexCount;
    public uint IndicesCount;
    public Bounds3D Bounds;
}