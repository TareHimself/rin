using System.Numerics;

namespace experiment.StencilAndCover.Rendering;

// Emits a triangle fan from a flattened contour's first vertex. No real CPU tessellation is
// needed — concavity, self-intersection, and holes (opposite-wound contours) are all resolved
// correctly by the GPU's stencil winding-count accumulation, not by triangle shape here.
internal static class FanTriangulator
{
    public static void EmitFan(IReadOnlyList<Vector2> polygon, List<Vector2> output)
    {
        if (polygon.Count < 3) return;

        var anchor = polygon[0];
        for (var i = 1; i < polygon.Count - 1; i++)
        {
            output.Add(anchor);
            output.Add(polygon[i]);
            output.Add(polygon[i + 1]);
        }
    }
}
