using Rin.Core.Shared;

namespace Rin.World.Graphics.Mesh;

public static class Extensions
{
    public static Bounds3D ComputeBounds(this ReadOnlySpan<Vertex> vertices)
    {
        var bounds = new Bounds3D();

        foreach (var vertex in vertices)
        {
            var location = vertex.Location;
            bounds.Update(location);
        }

        return bounds;
    }
}