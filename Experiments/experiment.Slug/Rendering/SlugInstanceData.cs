using System.Numerics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace experiment.Slug.Rendering;

// Per-instance draw data for one vector shape/glyph, uploaded verbatim to the GPU.
// Must match struct SlugInstanceData in slug.slang field-for-field.
[StructLayout(LayoutKind.Sequential)]
[NoReorder]
public struct SlugInstanceData
{
    // Screen-space quad the vertex shader spans (6 vertices, 2 triangles).
    public required Vector2 MinPos;
    public required Vector2 MaxPos;

    // Em-space coordinates at the same two corners, interpolated per-pixel in the fragment
    // shader to give each sample its glyph/shape-relative position.
    public required Vector2 MinEm;
    public required Vector2 MaxEm;

    // em-coordinate -> band-index transform: bandIndex = emCoord * Banding.xy + Banding.zw.
    public required Vector4 Banding;

    public required int ShapeLocX;
    public required int ShapeLocY;
    public required int BandMaxX;
    public required int BandMaxY;

    public required Vector4 Color;
}
