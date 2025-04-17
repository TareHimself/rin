namespace Rin.Engine.World.Mesh.Skinning;

public static class SkinnedVertexExtensions
{
    public static Bounds3D ComputeBounds(this IEnumerable<SkinnedVertex> vertices)
    {
        var bounds = new Bounds3D();

        foreach (var vertex in vertices)
        {
            var location = vertex.Vertex.Location;
            bounds.X.Max = float.Max(bounds.X.Max, location.X);
            bounds.X.Min = float.Min(bounds.X.Min, location.X);
            bounds.Y.Max = float.Max(bounds.Y.Max, location.Y);
            bounds.Y.Min = float.Min(bounds.Y.Min, location.Y);
            bounds.Z.Max = float.Max(bounds.Z.Max, location.Z);
            bounds.Z.Min = float.Min(bounds.Z.Min, location.Z);
        }

        return bounds;
    }
}