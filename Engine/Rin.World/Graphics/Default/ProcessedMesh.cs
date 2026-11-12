using System.Diagnostics;
using System.Numerics;
using Rin.Core.Graphics;
using Rin.Core.Shared;
using Rin.Shade;
using Rin.World.Graphics.Default.Shaders;
using Rin.World.Graphics.Mesh;

namespace Rin.World.Graphics.Default;

/// <summary>
///     Final mesh after processing i.e. skinning or static meshes, split into surfaces
/// </summary>
public class ProcessedMesh
{
    public required int AbsoluteMeshIndex;
    /// <summary>
    /// Mesh id within its group i.e. static vs skinned
    /// </summary>
    public required int GeometryId;
    public required Matrix4x4 Transform { get; set; }
    public required int SurfaceIndex { get; set; }
    
    public required MeshSurface Surface { get; set; }
    public required DeviceBufferView IndexBuffer { get; set; }
    public required uint IndexBufferId { get; set; }
    public required uint SourceVertexBufferId { get; set; }
    
    /// <summary>
    ///     The surface's vertex range; repointed at the skinning output for skinned meshes.
    /// </summary>
    public required DeviceBufferView VertexBuffer { get; set; }
    public required IMeshMaterial Material { get; set; }

    public MeshInstance ToInstance() => new()
    {
        Transform = Transform,
        Vertices = VertexBuffer.ToDeviceSpan<Vertex>()
    };
}