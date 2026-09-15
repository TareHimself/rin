namespace experiment.Slug.Rendering;

// Packs a VectorPath into the banded acceleration structure the SLUG shader walks per pixel.
// Ported from the band-partitioning scheme described in the Slug README ("Tips and Tricks")
// and the JCGT paper: dividing a shape's bounding box into horizontal and vertical strips
// (bands) so a pixel only tests the handful of curves overlapping its own strip instead of
// every curve in the shape.
internal static class ShapeBandPacker
{
    // Small em-space overlap so a curve sitting exactly on a band boundary is never dropped
    // from either neighboring band — the value recommended by the Slug README.
    private const float BandOverlapEpsilon = 1f / 1024f;

    public static PackedShape Pack(VectorPath path, int bandsX = 8, int bandsY = 8)
    {
        var curves = path.Curves;
        var boundsMin = path.BoundsMin;
        var boundsMax = path.BoundsMax;

        var curveTexels = new float[curves.Count * 8];
        for (var i = 0; i < curves.Count; i++)
        {
            var c = curves[i];
            var b = i * 8;
            curveTexels[b + 0] = c.P0.X;
            curveTexels[b + 1] = c.P0.Y;
            curveTexels[b + 2] = c.P1.X;
            curveTexels[b + 3] = c.P1.Y;
            curveTexels[b + 4] = c.P2.X;
            curveTexels[b + 5] = c.P2.Y;
            curveTexels[b + 6] = 0f;
            curveTexels[b + 7] = 0f;
        }

        // Horizontal bands partition Y and hold curves tested against a horizontal ray
        // (crossings found by scanning for where the curve's y equals the sample's y).
        // Flat-Y (horizontal-line) curves can never cross such a ray and are excluded —
        // straight from the Slug README.
        var hBands = BuildBands(
            curves, bandsY,
            axisMin: boundsMin.Y, axisMax: boundsMax.Y,
            getMin: c => MathF.Min(c.P0.Y, MathF.Min(c.P1.Y, c.P2.Y)),
            getMax: c => MathF.Max(c.P0.Y, MathF.Max(c.P1.Y, c.P2.Y)),
            isDegenerate: c => c.IsFlatY,
            sortKeyDescending: c => MathF.Max(c.P0.X, MathF.Max(c.P1.X, c.P2.X)));

        // Vertical bands partition X and hold curves tested against a vertical ray.
        // Flat-X (vertical-line) curves are excluded for the same reason.
        var vBands = BuildBands(
            curves, bandsX,
            axisMin: boundsMin.X, axisMax: boundsMax.X,
            getMin: c => MathF.Min(c.P0.X, MathF.Min(c.P1.X, c.P2.X)),
            getMax: c => MathF.Max(c.P0.X, MathF.Max(c.P1.X, c.P2.X)),
            isDegenerate: c => c.IsFlatX,
            sortKeyDescending: c => MathF.Max(c.P0.Y, MathF.Max(c.P1.Y, c.P2.Y)));

        return new PackedShape
        {
            CurveTexels = curveTexels,
            HBandCurves = hBands,
            VBandCurves = vBands,
            BandCountX = bandsX,
            BandCountY = bandsY,
            BoundsMin = boundsMin,
            BoundsMax = boundsMax
        };
    }

    private static List<int>[] BuildBands(
        IReadOnlyList<QuadraticBezier> curves,
        int count,
        float axisMin,
        float axisMax,
        Func<QuadraticBezier, float> getMin,
        Func<QuadraticBezier, float> getMax,
        Func<QuadraticBezier, bool> isDegenerate,
        Func<QuadraticBezier, float> sortKeyDescending)
    {
        var bands = new List<int>[count];
        for (var i = 0; i < count; i++) bands[i] = [];

        var span = MathF.Max(axisMax - axisMin, 1e-4f);
        var bandSize = span / count;

        for (var ci = 0; ci < curves.Count; ci++)
        {
            var curve = curves[ci];
            if (isDegenerate(curve)) continue;

            var lo = getMin(curve) - BandOverlapEpsilon;
            var hi = getMax(curve) + BandOverlapEpsilon;

            var first = Math.Clamp((int)MathF.Floor((lo - axisMin) / bandSize), 0, count - 1);
            var last = Math.Clamp((int)MathF.Floor((hi - axisMin) / bandSize), 0, count - 1);

            for (var b = first; b <= last; b++) bands[b].Add(ci);
        }

        foreach (var band in bands)
            band.Sort((a, b) => sortKeyDescending(curves[b]).CompareTo(sortKeyDescending(curves[a])));

        return bands;
    }
}
