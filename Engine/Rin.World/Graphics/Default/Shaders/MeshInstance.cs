using System.Numerics;
using JetBrains.Annotations;
using Rin.Shade;
using Rin.World.Graphics.Mesh;

namespace Rin.World.Graphics.Default.Shaders;

[NoReorder]
public struct MeshInstance
{
    public Matrix4x4 Transform;
    public BufferRef<Vertex> Vertices;
}
