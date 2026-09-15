using System.Numerics;
using System.Runtime.InteropServices;

namespace misc.VectorRendering.Slug;

// Per-instance draw data for a single vector shape or glyph.
// This struct is uploaded verbatim to the GPU and must match GlyphDrawData in slug.slang exactly.
// Fields are laid out sequentially without padding (all naturally aligned).
[StructLayout(LayoutKind.Sequential)]
public struct GlyphDrawData
{
    // Screen-space axis-aligned bounding box for this glyph's quad.
    // The vertex shader generates 6 vertices spanning [MinPos, MaxPos].
    public Vector2 MinPos;  // top-left in screen pixels
    public Vector2 MaxPos;  // bottom-right in screen pixels

    // Em-space coordinates at the quad corners — interpolated across the quad
    // in the fragment shader to give each pixel its glyph-relative position.
    // Typically MinEm = glyph BoundsMin, MaxEm = glyph BoundsMax (possibly expanded
    // by 1 pixel to ensure boundary pixels are covered).
    public Vector2 MinEm;
    public Vector2 MaxEm;

    // Banding transform: maps em-space position to band indices.
    //   bandIndex.x = emCoord.x * Banding.X + Banding.Z  (which vertical band)
    //   bandIndex.y = emCoord.y * Banding.Y + Banding.W  (which horizontal band)
    public Vector4 Banding;

    // Location of this shape's data block in the band texture.
    // These are the (x, y) integer texel coordinates passed to CalcBandLoc in the shader.
    public int ShapeLocX;
    public int ShapeLocY;

    // Maximum valid band index (numBandsX/Y - 1) used for clamping in the shader.
    public int BandMaxX;
    public int BandMaxY;

    // RGBA color multiplied with the coverage to produce the final pixel color.
    public Vector4 Color;
}
