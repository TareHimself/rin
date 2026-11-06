using System.Numerics;
using Rin.Shade;
using Rin.World.Graphics.Default.Shaders;

namespace Rin.World.Graphics.Surfaces;

/// <summary>
///     The vertex math every mesh pipeline shares, so the depth prepass and the color pass run the same ops and agree
///     on depth exactly.
/// </summary>
[ShadeExport]
public static class SurfaceVertices
{
    public static WorldVertex Load(MeshInstance instance, int vertexId)
    {
        var vertex = instance.Vertices[vertexId];

        WorldVertex world;
        world.Position = Vector4.Transform(new Vector4(vertex.Location, 1f), instance.Transform).XYZ;
        world.Normal = ShaderMath.TransformNormal(vertex.Normal, instance.Transform);
        world.Uv = vertex.UV;
        return world;
    }

    public static Vector4 Project(Vector3 world, Matrix4x4 view, Matrix4x4 projection)
    {
        var viewPosition = Vector4.Transform(new Vector4(world, 1f), view);
        return Vector4.Transform(viewPosition, projection);
    }
}
