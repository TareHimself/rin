using System.Numerics;

namespace misc.VectorRendering.Slug;

// A VectorPath is a closed 2D outline expressed as quadratic bezier curves.
// This is the central abstraction for the SLUG vector rendering system.
// Any shape can be a VectorPath: font glyphs, circles approximated with arcs,
// rounded rectangles, imported SVG paths, etc.
// The SLUG GPU algorithm operates directly on this representation.
public record VectorPath(IReadOnlyList<Bezier> Curves, Vector2 BoundsMin, Vector2 BoundsMax)
{
    // Build a VectorPath from an arbitrary list of bezier curves.
    // BoundsMin/BoundsMax are computed conservatively using the control-point
    // convex hull (guaranteed to contain the actual bezier geometry).
    public static VectorPath FromCurves(IReadOnlyList<Bezier> curves)
    {
        if (curves.Count == 0)
            return new VectorPath(curves, Vector2.Zero, Vector2.Zero);

        var min = new Vector2(float.MaxValue);
        var max = new Vector2(float.MinValue);

        foreach (var c in curves)
        {
            min = Vector2.Min(min, Vector2.Min(c.Begin, Vector2.Min(c.Control, c.End)));
            max = Vector2.Max(max, Vector2.Max(c.Begin, Vector2.Max(c.Control, c.End)));
        }

        // Expand by a small margin to ensure curves at the exact boundary don't get clipped
        // by the band assignment rounding.
        var margin = new Vector2(0.5f);
        return new VectorPath(curves, min - margin, max + margin);
    }
}
