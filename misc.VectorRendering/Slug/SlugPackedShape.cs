using System.Numerics;

namespace misc.VectorRendering.Slug;

// The output of SlugShapePacker.Pack(): CPU-side packed data for one VectorPath,
// ready to be stitched into the global atlas texture arrays by SlugAtlas.AddShape().
//
// We separate "what curves exist" (CurveTexels) from "which curves go in each band"
// (HBandCurves / VBandCurves) so that SlugAtlas can translate local curve indices
// into global atlas coordinates during stitching.
internal sealed class SlugPackedShape
{
    // Raw RGBA32F texel data for the curve texture.
    // Each bezier occupies 2 consecutive texels:
    //   Texel 0: (p1.x, p1.y, p2.x, p2.y)
    //   Texel 1: (p3.x, p3.y, 0,    0   )
    // Length = Curves.Count * 8 floats.
    public required float[] CurveTexels;

    // Per horizontal-band list of local curve indices (0-based into the shape's own curves).
    // Index i corresponds to horizontal band i (partitioned along the Y axis).
    // Within each band, curves are sorted DESCENDING by their maximum X coordinate —
    // this lets the shader break early once the remaining curves are all to the right.
    public required List<int>[] HBandCurves;

    // Per vertical-band list of local curve indices.
    // Sorted DESCENDING by max Y for the shader's early-exit check.
    public required List<int>[] VBandCurves;

    // Number of vertical bands (corresponds to VBandCurves.Length).
    public required int BandCountX;

    // Number of horizontal bands (corresponds to HBandCurves.Length).
    public required int BandCountY;

    public required Vector2 BoundsMin;
    public required Vector2 BoundsMax;
}
