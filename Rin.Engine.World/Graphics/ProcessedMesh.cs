using System.Numerics;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.Meshes;

namespace Rin.Engine.World.Graphics;

/// <summary>
/// Final mesh after processing i.e. skinning or static meshes, split into surfaces
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
}