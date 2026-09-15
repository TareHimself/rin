using System.Numerics;

namespace experiment.Slug.Rendering;

// A closed 2D outline expressed as quadratic Bezier curves — the shape unit SLUG renders.
// Any number of contours can be merged into one VectorPath (e.g. the outer ring and the
// inner hole of a glyph like 'O'); the nonzero winding rule in the shader handles the holes.
public sealed record VectorPath(IReadOnlyList<QuadraticBezier> Curves, Vector2 BoundsMin, Vector2 BoundsMax)
{
    // Build a VectorPath from an arbitrary list of curves, computing a conservative bounding
    // box from the control-point convex hull (always contains the true curve geometry) with a
    // small margin so boundary-touching curves aren't clipped by band-assignment rounding.
    public static VectorPath FromCurves(IReadOnlyList<QuadraticBezier> curves)
    {
        if (curves.Count == 0) return new VectorPath(curves, Vector2.Zero, Vector2.Zero);

        var min = new Vector2(float.MaxValue);
        var max = new Vector2(float.MinValue);
        foreach (var c in curves)
        {
            min = Vector2.Min(min, c.Min);
            max = Vector2.Max(max, c.Max);
        }

        var margin = new Vector2(0.5f);
        return new VectorPath(curves, min - margin, max + margin);
    }
}
