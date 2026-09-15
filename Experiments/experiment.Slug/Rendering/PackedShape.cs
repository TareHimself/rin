using System.Numerics;

namespace experiment.Slug.Rendering;

// CPU-packed, atlas-ready output of ShapeBandPacker.Pack() for one VectorPath.
// "What curves exist" (CurveTexels) is kept separate from "which band holds which curve"
// (HBandCurves/VBandCurves, as local 0-based curve indices) so SlugAtlas can translate the
// local indices into absolute atlas-texture coordinates when it stitches this shape in.
internal sealed class PackedShape
{
    // Curve texture payload: 2 RGBA32F texels per curve —
    //   texel 0 = (p0.x, p0.y, p1.x, p1.y), texel 1 = (p2.x, p2.y, 0, 0) — matches the Slug
    // reference curve-texture layout exactly.
    public required float[] CurveTexels;

    // HBandCurves[b] / VBandCurves[b]: local curve indices assigned to horizontal band b /
    // vertical band b, each sorted descending by max-x (H) or max-y (V) for the shader's
    // early-exit loop break.
    public required List<int>[] HBandCurves;
    public required List<int>[] VBandCurves;

    public required int BandCountX;
    public required int BandCountY;

    public required Vector2 BoundsMin;
    public required Vector2 BoundsMax;
}
