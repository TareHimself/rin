namespace Rin.Engine.World.Mesh.Skinning;

public static class SkinnedVertexExtensions
{
    public static Bounds3D ComputeBounds(this IEnumerable<SkinnedVertex> vertices)
    {
        var bounds = new Bounds3D();

        foreach (var vertex in vertices)
        {
            var location = vertex.Vertex.Location;
            bounds.Update(location);
        }

        return bounds;
    }
}