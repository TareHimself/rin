using System.Numerics;

namespace misc.VectorRendering.Slug;

// Converts a VectorPath into the packed representation used by the SLUG atlas.
// This is a CPU-only preprocessing step; the output feeds directly into SlugAtlas.
//
// The SLUG algorithm accelerates per-pixel winding number computation by dividing
// each shape's bounding box into a regular grid of bands. Horizontal bands (strips
// across the y-axis) hold the curves that could be crossed by a leftward horizontal
// ray from any pixel in that strip. Vertical bands (strips across the x-axis) hold
// curves for downward vertical rays. Sorting each band's curve list by decreasing
// max-x (or max-y) lets the GPU break the iteration loop early.
//
// Reference: Lengyel, E. "GPU-Accelerated Path Rendering", JCGT vol 6 no 2, 2017.
internal static class SlugShapePacker
{
    // Pack a VectorPath into atlas-ready data.
    // bandsX = number of vertical bands (columns); default 8 gives good perf/quality trade-off.
    // bandsY = number of horizontal bands (rows).
    public static SlugPackedShape Pack(VectorPath path, int bandsX = 8, int bandsY = 8)
    {
        var curves    = path.Curves;
        var boundsMin = path.BoundsMin;
        var boundsMax = path.BoundsMax;

        // Guard against degenerate bounding boxes so we never divide by zero.
        var size = boundsMax - boundsMin;
        if (size.X < 1e-4f) size = size with { X = 1f };
        if (size.Y < 1e-4f) size = size with { Y = 1f };

        // ---- Step 1: Pack curve texels ----
        // Each curve → 2 RGBA texels = 8 floats.
        // We store absolute glyph-relative coordinates; the shader shifts by the
        // current sample position to get pixel-relative coordinates.
        var curveTexels = new float[curves.Count * 8];
        for (var ci = 0; ci < curves.Count; ci++)
        {
            var c    = curves[ci];
            var base_ = ci * 8;
            // Texel 0: control points 1 and 2 packed as (x1, y1, x2, y2)
            curveTexels[base_ + 0] = c.Begin.X;
            curveTexels[base_ + 1] = c.Begin.Y;
            curveTexels[base_ + 2] = c.Control.X;
            curveTexels[base_ + 3] = c.Control.Y;
            // Texel 1: control point 3, padding zeros
            curveTexels[base_ + 4] = c.End.X;
            curveTexels[base_ + 5] = c.End.Y;
            curveTexels[base_ + 6] = 0f;
            curveTexels[base_ + 7] = 0f;
        }

        // ---- Step 2: Assign curves to horizontal bands ----
        // Horizontal bands partition the y-axis into bandsY equal strips.
        // A curve belongs to band b if its y-extent overlaps the band's y-range.
        // We use conservative overlap based on the control-point bounding box
        // (the actual bezier is always within the convex hull of its control points).
        // Sorted descending by max-x so the GPU can skip all remaining curves
        // once the rightmost curve in the list is beyond the pixel.
        var hBandCurves = BuildBands(
            curves, bandsY,
            getMin:  c => MathF.Min(c.Begin.Y, MathF.Min(c.Control.Y, c.End.Y)),
            getMax:  c => MathF.Max(c.Begin.Y, MathF.Max(c.Control.Y, c.End.Y)),
            axisMin: boundsMin.Y,
            axisMax: boundsMax.Y,
            sortBy:  c => -MathF.Max(c.Begin.X, MathF.Max(c.Control.X, c.End.X)));

        // ---- Step 3: Assign curves to vertical bands ----
        // Vertical bands partition the x-axis into bandsX equal strips.
        // Sorted descending by max-y so the GPU can skip curves above the pixel.
        var vBandCurves = BuildBands(
            curves, bandsX,
            getMin:  c => MathF.Min(c.Begin.X, MathF.Min(c.Control.X, c.End.X)),
            getMax:  c => MathF.Max(c.Begin.X, MathF.Max(c.Control.X, c.End.X)),
            axisMin: boundsMin.X,
            axisMax: boundsMax.X,
            sortBy:  c => -MathF.Max(c.Begin.Y, MathF.Max(c.Control.Y, c.End.Y)));

        return new SlugPackedShape
        {
            CurveTexels  = curveTexels,
            HBandCurves  = hBandCurves,
            VBandCurves  = vBandCurves,
            BandCountX   = bandsX,
            BandCountY   = bandsY,
            BoundsMin    = boundsMin,
            BoundsMax    = boundsMax
        };
    }

    // Partition the curves into `count` equal bands along the axis defined by [axisMin, axisMax].
    // Each curve is added to every band whose range overlaps the curve's extent on that axis.
    // Within each band, curves are sorted by `sortBy` ascending (use negative values for descending).
    private static List<int>[] BuildBands(
        IReadOnlyList<Bezier> curves,
        int                   count,
        Func<Bezier, float>   getMin,
        Func<Bezier, float>   getMax,
        float                 axisMin,
        float                 axisMax,
        Func<Bezier, float>   sortBy)
    {
        var bands    = new List<int>[count];
        var bandSize = (axisMax - axisMin) / count;

        for (var i = 0; i < count; i++) bands[i] = [];

        for (var ci = 0; ci < curves.Count; ci++)
        {
            var c    = curves[ci];
            var cMin = getMin(c);
            var cMax = getMax(c);

            // Compute the first and last bands that this curve's extent overlaps.
            // Both use Floor: band k covers [axisMin + k*bandSize, axisMin + (k+1)*bandSize).
            // A curve belongs to the last band whose lower edge is <= cMax, which is
            // floor((cMax-axisMin)/bandSize). Ceiling would incorrectly include one extra
            // empty band whenever cMax lands between band boundaries.
            var first = Math.Max(0, (int)MathF.Floor((cMin - axisMin) / bandSize));
            var last  = Math.Min(count - 1, (int)MathF.Floor((cMax - axisMin) / bandSize));

            for (var b = first; b <= last; b++)
                bands[b].Add(ci);
        }

        // Sort each band so the shader can do early exit.
        foreach (var band in bands)
            band.Sort((a, b) => sortBy(curves[a]).CompareTo(sortBy(curves[b])));

        return bands;
    }
}
