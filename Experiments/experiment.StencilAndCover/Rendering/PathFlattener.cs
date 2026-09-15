using System.Numerics;

namespace experiment.StencilAndCover.Rendering;

// Converts a Contour's quadratic-Bezier edges into a polygon (line loop) for the stencil-fill
// fan, via adaptive de Casteljau subdivision. This polygon is an approximation only — it feeds
// the cheap interior fill; boundary_aa.slang corrects the boundary against the true curves.
internal static class PathFlattener
{
    private const int MaxDepth = 16;

    public static List<Vector2> Flatten(Contour contour, float tolerance = 0.25f)
    {
        var output = new List<Vector2>();
        if (contour.Segments.Count == 0) return output;

        output.Add(contour.Segments[0].P0);
        foreach (var segment in contour.Segments)
            FlattenSegment(segment.P0, segment.Control, segment.P2, output, tolerance, 0);

        return output;
    }

    private static void FlattenSegment(Vector2 p0, Vector2 p1, Vector2 p2, List<Vector2> output, float tolerance,
        int depth)
    {
        if (depth >= MaxDepth || IsFlatEnough(p0, p1, p2, tolerance))
        {
            output.Add(p2);
            return;
        }

        // de Casteljau split at t = 0.5.
        var p01 = (p0 + p1) * 0.5f;
        var p12 = (p1 + p2) * 0.5f;
        var p012 = (p01 + p12) * 0.5f;

        FlattenSegment(p0, p01, p012, output, tolerance, depth + 1);
        FlattenSegment(p012, p12, p2, output, tolerance, depth + 1);
    }

    // Perpendicular distance from the control point to the p0-p2 chord, as a proxy for how much
    // the curve deviates from a straight line.
    private static bool IsFlatEnough(Vector2 p0, Vector2 p1, Vector2 p2, float tolerance)
    {
        var chord = p2 - p0;
        var chordLength = chord.Length();
        if (chordLength < 1e-6f) return true;

        var cross = MathF.Abs(chord.X * (p1.Y - p0.Y) - chord.Y * (p1.X - p0.X));
        return cross / chordLength <= tolerance;
    }
}
