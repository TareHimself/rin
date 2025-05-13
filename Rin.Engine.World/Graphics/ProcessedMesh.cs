using System.Numerics;
using Rin.Engine.Graphics;

namespace Rin.Engine.World.Graphics;

/// <summary>
///     Final mesh after processing i.e. skinning or static meshes, split into surfaces
/// </summary>
public class ProcessedMesh
{
    public required Matrix4x4 Transform { get; set; }

    public required uint IndicesCount { get; set; }
    public required uint IndicesStart { get; set; }
    public required uint VertexCount { get; set; }
    public required uint VertexStart { get; set; }
    public required IDeviceBufferView IndexBuffer { get; set; }
    public required IDeviceBufferView VertexBuffer { get; set; }
    public required IMeshMaterial Material { get; set; }

    public required Bounds3D Bounds { get; set; }

    public class CompareByIndexAndMaterial : IEqualityComparer<ProcessedMesh>
    {
        public bool Equals(ProcessedMesh? x, ProcessedMesh? y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            if (x.Material.GetType() != y.Material.GetType()) return false;
            if (x.IndexBuffer.NativeBuffer != y.IndexBuffer.NativeBuffer) return false;
            if (x.IndexBuffer.Offset != y.IndexBuffer.Offset) return false;
            if (x.IndexBuffer.Size != y.IndexBuffer.Size) return false;
            return true;
        }

        public int GetHashCode(ProcessedMesh obj)
        {
            return HashCode.Combine(obj.Material.GetType(), obj.IndexBuffer.Size, obj.IndexBuffer.Offset,
                obj.IndexBuffer.NativeBuffer);
        }
    }

    public class CompareByVertexCountAndOffset : IEqualityComparer<ProcessedMesh>
    {
        public bool Equals(ProcessedMesh? x, ProcessedMesh? y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            if (x.VertexCount != y.VertexCount) return false;
            if (x.VertexStart != y.VertexStart) return false;
            return true;
        }

        public int GetHashCode(ProcessedMesh obj)
        {
            return HashCode.Combine(obj.VertexCount, obj.VertexStart);
        }
    }
}