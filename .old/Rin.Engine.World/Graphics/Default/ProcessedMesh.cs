using System.Diagnostics;
using System.Numerics;
using Rin.Framework.Graphics;

namespace Rin.Engine.World.Graphics.Default;

/// <summary>
///     Final mesh after processing i.e. skinning or static meshes, split into surfaces
/// </summary>
public class ProcessedMesh
{
    public required int Id;
    public required Matrix4x4 Transform { get; set; }

    public required uint IndicesCount { get; set; }
    public required uint IndicesStart { get; set; }
    public required uint VertexCount { get; set; }
    public required uint VertexStart { get; set; }
    public required DeviceBufferView IndexBuffer { get; set; }
    public required DeviceBufferView VertexBuffer { get; set; }
    public required IMeshMaterial Material { get; set; }

    public required Bounds3D Bounds { get; set; }

    public class CompareByIndexAndMaterial : IEqualityComparer<ProcessedMesh>
    {
        public bool Equals(ProcessedMesh? x, ProcessedMesh? y)
        {
           
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            Debug.Assert(x.IndexBuffer.IsValid);
            Debug.Assert(y.IndexBuffer.IsValid);
            if (x.Material.GetType() != y.Material.GetType()) return false;
            if (x.IndexBuffer.Buffer.NativeBuffer != y.IndexBuffer.Buffer.NativeBuffer) return false;
            if (x.IndexBuffer.Offset != y.IndexBuffer.Offset) return false;
            if (x.IndexBuffer.Size != y.IndexBuffer.Size) return false;
            return true;
        }

        public int GetHashCode(ProcessedMesh obj)
        {
            Debug.Assert(obj.IndexBuffer.IsValid,"IndexBuffer is not valid");
            return HashCode.Combine(obj.Material.GetType(), obj.IndexBuffer.Size, obj.IndexBuffer.Offset,
                obj.IndexBuffer.Buffer.NativeBuffer);
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


    /// <summary>
    ///     Groups together meshes that can be drawn in one vkCmdDrawIndexed
    /// </summary>
    public class CompareBatchable : IEqualityComparer<ProcessedMesh>
    {
        public bool Equals(ProcessedMesh? x, ProcessedMesh? y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            if (x.VertexCount != y.VertexCount) return false;
            if (x.VertexStart != y.VertexStart) return false;
            Debug.Assert(x.IndexBuffer.IsValid);
            Debug.Assert(y.IndexBuffer.IsValid);
            if (x.Material.ColorPass.Shader != y.Material.ColorPass.Shader) return false;
            if (x.IndexBuffer.Buffer.NativeBuffer != y.IndexBuffer.Buffer.NativeBuffer) return false;
            if (x.IndexBuffer.Offset != y.IndexBuffer.Offset) return false;
            if (x.IndexBuffer.Size != y.IndexBuffer.Size) return false;
            return true;
        }

        public int GetHashCode(ProcessedMesh obj)
        {
            Debug.Assert(obj.IndexBuffer.IsValid, "IndexBuffer is not valid");
            return HashCode.Combine(obj.Material.ColorPass.Shader, obj.IndexBuffer.Size, obj.IndexBuffer.Offset,
                obj.IndexBuffer.Buffer.NativeBuffer, obj.VertexCount, obj.VertexStart);
        }
    }

    public class CompareBatchableDepth : IEqualityComparer<ProcessedMesh>
    {
        public bool Equals(ProcessedMesh? x, ProcessedMesh? y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            if (x.VertexCount != y.VertexCount) return false;
            if (x.VertexStart != y.VertexStart) return false;
            Debug.Assert(x.IndexBuffer.IsValid);
            Debug.Assert(y.IndexBuffer.IsValid);
            if (x.Material.DepthPass.Shader != y.Material.DepthPass.Shader) return false;
            if (x.IndexBuffer.Buffer.NativeBuffer != y.IndexBuffer.Buffer.NativeBuffer) return false;
            if (x.IndexBuffer.Offset != y.IndexBuffer.Offset) return false;
            if (x.IndexBuffer.Size != y.IndexBuffer.Size) return false;
            return true;
        }

        public int GetHashCode(ProcessedMesh obj)
        {
            Debug.Assert(obj.IndexBuffer.IsValid, "IndexBuffer is not valid");
            return HashCode.Combine(obj.Material.DepthPass.Shader, obj.IndexBuffer.Size, obj.IndexBuffer.Offset,
                obj.IndexBuffer.Buffer.NativeBuffer, obj.VertexCount, obj.VertexStart);
        }
    }


    public class CompareIndirectBatch : IEqualityComparer<ProcessedMesh>
    {
        public bool Equals(ProcessedMesh? x, ProcessedMesh? y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            Debug.Assert(x.IndexBuffer.IsValid);
            Debug.Assert(y.IndexBuffer.IsValid);
            return x.IndexBuffer == y.IndexBuffer && x.Material.IsBatchable(false,y.Material);
        }

        public int GetHashCode(ProcessedMesh obj)
        {
            Debug.Assert(obj.IndexBuffer.IsValid, "IndexBuffer is not valid");
            return HashCode.Combine(obj.Material.ColorPass.Shader, obj.IndexBuffer.Size, obj.IndexBuffer.Offset,
                obj.IndexBuffer.Buffer.NativeBuffer);
        }
    }

    public class CompareIndirectBatchDepth : IEqualityComparer<ProcessedMesh>
    {
        public bool Equals(ProcessedMesh? x, ProcessedMesh? y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            Debug.Assert(x.IndexBuffer.IsValid);
            Debug.Assert(y.IndexBuffer.IsValid);
            return x.IndexBuffer == y.IndexBuffer && x.Material.IsBatchable(true,y.Material);
        }

        public int GetHashCode(ProcessedMesh obj)
        {
            Debug.Assert(obj.IndexBuffer.IsValid, "IndexBuffer is not valid");
            return HashCode.Combine(obj.Material.DepthPass.Shader, obj.IndexBuffer.Size, obj.IndexBuffer.Offset,
                obj.IndexBuffer.Buffer.NativeBuffer);
        }
    }
}