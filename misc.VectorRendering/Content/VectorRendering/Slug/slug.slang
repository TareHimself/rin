// slug.slang — SLUG GPU Vector Rendering
// Implementation of the SLUG algorithm by Eric Lengyel.
// Reference: "GPU-Accelerated Path Rendering", JCGT vol 6 no 2, 2017.
// The SLUG patent expired in March 2026 making this implementation public domain.
//
// ALGORITHM OVERVIEW
// ==================
// For each pixel inside a shape's bounding quad, SLUG casts two rays:
//   - A leftward horizontal ray  (in the −x direction from the pixel)
//   - A downward vertical ray    (in the −y direction from the pixel)
//
// For each ray, the shader counts signed bezier crossings: +1 for upward/rightward
// crossings (counterclockwise winding) and −1 for downward/leftward crossings.
// The two signed winding counts are averaged to produce a smooth coverage value.
//
// To avoid iterating ALL curves for every pixel, each shape's bounding box is divided
// into a grid of bands (stored in the band texture). A pixel only iterates the curves
// that overlap its horizontal/vertical strip — typically 3-5 curves vs. 50-200 total.
//
// TEXTURE FORMATS (both RGBA32F, width=4096)
// ============================================
// Curve texture: 2 texels per bezier curve
//   Texel 0: (p1.x, p1.y, p2.x, p2.y)
//   Texel 1: (p3.x, p3.y, 0,    0   )
//
// Band texture: one block per shape starting at its (glyphLoc.x, glyphLoc.y).
//   Within the block (all offsets via CalcBandLoc):
//   [0 .. bandsY-1]              H band headers: (count, listOffset, 0, 0)
//   [bandsY .. bandsY+bandsX-1]  V band headers: same
//   [listOffset for band b]      Curve index entries: (curveTexX, curveTexY, 0, 0)

#include "Core/Shaders/Attributes/blending.slang"
#include "Core/Shaders/images.slang"

// Band texture row width — must match SlugAtlas.TextureWidth in C#.
static const int kLogBandWidth = 12;       // 2^12 = 4096
static const int kBandWidth    = 4096;

// Translate a linear texel offset (from a shape's band-texture origin) into
// absolute 2D coordinates, wrapping at kBandWidth columns.
// This mirrors how the CPU packs the band data with CalcBandLoc.
int2 CalcBandLoc(int2 glyphLoc, int offset)
{
    int absolute = glyphLoc.x + offset;
    return int2(absolute & (kBandWidth - 1), glyphLoc.y + (absolute >> kLogBandWidth));
}

// Determine which roots of the bezier's quadratic at² − 2bt + c = 0 correspond
// to genuine crossings of the ray's axis through the pixel.
//
// The sign pattern of the three coordinates (relative to the pixel) encodes the
// curve's topology: how many times it crosses zero and in which direction.
// The 8-entry lookup table 0x2E74 was derived by Lengyel from the nine possible
// sign combinations; it returns a 2-bit mask:
//   bit 0 = root t0 = (b+√D)/a  gives an upward/rightward crossing   (+1 to winding)
//   bit 1 = root t1 = (b−√D)/a  gives a downward/leftward crossing   (−1 to winding)
//
// Proof that t0 is always the positive-direction root:
//   B'(t) = 2(−b + t·a), so B'(t0) = 2s > 0 and B'(t1) = −2s < 0
//   where s = √(b²−ac) > 0.  The sign of the crossing is therefore
//   independent of the curve's shape — only the root index matters.
uint CalcRootCode(float y1, float y2, float y3)
{
    // Encode sign bits of the three coordinates as a 3-bit index.
    uint bits = (asuint(y1) >> 31u)
              | ((asuint(y2) >> 30u) & 2u)
              | ((asuint(y3) >> 29u) & 4u);
    return (0x2E74u >> (bits * 2u)) & 3u;
}

// Accumulate winding contributions from one bezier curve.
// p1, p2, p3 are the control points ALREADY SHIFTED so the pixel is at the origin.
// ppm = pixels-per-em (1 / fwidth(emCoord)) — controls the AA kernel width.
// winding.x = horizontal winding count (leftward ray)
// winding.y = vertical winding count (downward ray)
//
// The smooth step saturate(0.5 − x·ppm.x) gives:
//   1.0 when the crossing is clearly left of the pixel (x < −0.5/ppm.x)
//   0.5 when exactly at the pixel centre (x = 0)
//   0.0 when clearly right of the pixel (x > 0.5/ppm.x)
// This is the antialiasing kernel: a 1-pixel-wide ramp centred on the curve boundary.
float2 AccumWindingFromCurve(float2 winding, float2 p1, float2 p2, float2 p3, float2 ppm)
{
    // ---- Horizontal winding (leftward ray, y=0 crossings) ----
    {
        float a = p1.y - 2.0*p2.y + p3.y;
        float b = p1.y - p2.y;
        float c = p1.y;

        uint code = CalcRootCode(p1.y, p2.y, p3.y);
        if (code != 0u)
        {
            float disc = b*b - a*c;
            if (disc >= 0.0)
            {
                float s = sqrt(disc);

                // Degenerate case: a≈0 means the curve is approximately linear in y.
                // The quadratic degenerates to c - 2bt = 0, one root at t = c/(2b).
                // We must handle BOTH winding directions here — the original code
                // only handled code&1 (t0, upward), silently dropping all downward
                // crossings from line segments, which caused large gaps in filled shapes.
                if (abs(a) < 1e-6)
                {
                    if (abs(b) > 1e-6)
                    {
                        float t = c / (2.0 * b);
                        if (t >= 0.0 && t <= 1.0)
                        {
                            float x = lerp(lerp(p1.x, p2.x, t), lerp(p2.x, p3.x, t), t);
                            if ((code & 1u) != 0u) winding.x += saturate(0.5 - x * ppm.x);
                            if ((code & 2u) != 0u) winding.x -= saturate(0.5 - x * ppm.x);
                        }
                    }
                }
                else
                {
                    // t0 = (b+s)/a → upward crossing → +contribution to horizontal winding
                    if ((code & 1u) != 0u)
                    {
                        float t = (b + s) / a;
                        if (t >= 0.0 && t <= 1.0)
                        {
                            float x = lerp(lerp(p1.x, p2.x, t), lerp(p2.x, p3.x, t), t);
                            winding.x += saturate(0.5 - x * ppm.x);
                        }
                    }
                    // t1 = (b−s)/a → downward crossing → −contribution to horizontal winding
                    if ((code & 2u) != 0u)
                    {
                        float t = (b - s) / a;
                        if (t >= 0.0 && t <= 1.0)
                        {
                            float x = lerp(lerp(p1.x, p2.x, t), lerp(p2.x, p3.x, t), t);
                            winding.x -= saturate(0.5 - x * ppm.x);
                        }
                    }
                }
            }
        }
    }

    // ---- Vertical winding (downward ray, x=0 crossings) ----
    // Identical to horizontal but with x and y roles swapped.
    {
        float a = p1.x - 2.0*p2.x + p3.x;
        float b = p1.x - p2.x;
        float c = p1.x;

        uint code = CalcRootCode(p1.x, p2.x, p3.x);
        if (code != 0u)
        {
            float disc = b*b - a*c;
            if (disc >= 0.0)
            {
                float s = sqrt(disc);

                if (abs(a) < 1e-6)
                {
                    if (abs(b) > 1e-6)
                    {
                        float t = c / (2.0 * b);
                        if (t >= 0.0 && t <= 1.0)
                        {
                            float y = lerp(lerp(p1.y, p2.y, t), lerp(p2.y, p3.y, t), t);
                            if ((code & 1u) != 0u) winding.y += saturate(0.5 - y * ppm.y);
                            if ((code & 2u) != 0u) winding.y -= saturate(0.5 - y * ppm.y);
                        }
                    }
                }
                else
                {
                    // t0 = (b+s)/a → rightward crossing → +contribution to vertical winding
                    if ((code & 1u) != 0u)
                    {
                        float t = (b + s) / a;
                        if (t >= 0.0 && t <= 1.0)
                        {
                            float y = lerp(lerp(p1.y, p2.y, t), lerp(p2.y, p3.y, t), t);
                            winding.y += saturate(0.5 - y * ppm.y);
                        }
                    }
                    // t1 = (b−s)/a → leftward crossing → −contribution to vertical winding
                    if ((code & 2u) != 0u)
                    {
                        float t = (b - s) / a;
                        if (t >= 0.0 && t <= 1.0)
                        {
                            float y = lerp(lerp(p1.y, p2.y, t), lerp(p2.y, p3.y, t), t);
                            winding.y -= saturate(0.5 - y * ppm.y);
                        }
                    }
                }
            }
        }
    }

    return winding;
}

// Convert a 2D signed winding count (H, V) to a scalar coverage value in [0,1].
// Non-zero fill rule: inside if winding ≠ 0.
// We average the horizontal and vertical estimates for smoother results at corners.
float CalcCoverage(float2 winding)
{
    return (saturate(abs(winding.x)) + saturate(abs(winding.y))) * 0.5;
}

// Main SLUG per-pixel routine.
// emCoord:  the em-space coordinate of this pixel (interpolated from the quad's MinEm/MaxEm)
// banding:  (scaleX, scaleY, offsetX, offsetY) mapping em → band index
// shapeLoc: (x, y) of this shape's band-texture data block origin
// bandMax:  (bandMaxX, bandMaxY) = number_of_bands − 1 for clamping
float SlugRender(
    float2      emCoord,
    float4      banding,
    int2        shapeLoc,
    int2        bandMax,
    ImageHandle curveTexture,
    ImageHandle bandTexture)
{
    // Map this pixel's em-coordinate to horizontal and vertical band indices.
    int2 bandIdx = clamp(
        int2(emCoord * banding.xy + banding.zw),
        int2(0, 0), bandMax);

    // Pixels-per-em for the AA kernel width.
    // In a 2D orthographic renderer where 1 screen pixel = 1 em unit, fwidth returns ~(1,1)
    // and ppm ≈ (1,1) giving a 1-pixel-wide AA kernel at every scale.
    float2 ppm = 1.0 / max(fwidth(emCoord), float2(1e-4));

    float2 winding = float2(0.0);

    // ---- Horizontal bands ----
    // H band header for bandIdx.y is at offset bandIdx.y from the shape's block origin.
    {
        float4 header = bandTexture.TexelLoad(CalcBandLoc(shapeLoc, bandIdx.y));
        int count  = int(header.x);
        int offset = int(header.y);

        for (int i = 0; i < count; i++)
        {
            // Each entry is (curveTexelX, curveTexelY, 0, 0) pointing into the curve texture.
            float4 ref = bandTexture.TexelLoad(CalcBandLoc(shapeLoc, offset + i));
            int2 cloc  = int2(ref.xy);

            // Load two texels for this curve:
            //   t0: (p1.x, p1.y, p2.x, p2.y)   t1: (p3.x, p3.y, 0, 0)
            float4 ct0 = curveTexture.TexelLoad(cloc);
            float4 ct1 = curveTexture.TexelLoad(cloc + int2(1, 0));

            // Shift control points so this pixel is at the origin.
            float2 p1 = ct0.xy - emCoord;
            float2 p2 = ct0.zw - emCoord;
            float2 p3 = ct1.xy - emCoord;

            winding = AccumWindingFromCurve(winding, p1, p2, p3, ppm);
        }
    }

    // ---- Vertical bands ----
    // V band headers start after all H band headers: offset = (bandMax.y + 1) + bandIdx.x.
    {
        int vHeaderOffset = bandMax.y + 1 + bandIdx.x;
        float4 header = bandTexture.TexelLoad(CalcBandLoc(shapeLoc, vHeaderOffset));
        int count  = int(header.x);
        int offset = int(header.y);

        for (int i = 0; i < count; i++)
        {
            float4 ref = bandTexture.TexelLoad(CalcBandLoc(shapeLoc, offset + i));
            int2 cloc  = int2(ref.xy);

            float4 ct0 = curveTexture.TexelLoad(cloc);
            float4 ct1 = curveTexture.TexelLoad(cloc + int2(1, 0));

            float2 p1 = ct0.xy - emCoord;
            float2 p2 = ct0.zw - emCoord;
            float2 p3 = ct1.xy - emCoord;

            winding = AccumWindingFromCurve(winding, p1, p2, p3, ppm);
        }
    }

    return CalcCoverage(winding);
}

// ---- Per-instance draw data, matching GlyphDrawData in C# exactly ----
// ScalarDataLayout ensures field-by-field packing matching StructLayout.Sequential in C#.
struct GlyphDrawData
{
    float2 minPos;    // screen-space quad top-left
    float2 maxPos;    // screen-space quad bottom-right
    float2 minEm;     // em-space coordinate at top-left corner
    float2 maxEm;     // em-space coordinate at bottom-right corner
    float4 banding;   // (scaleX, scaleY, offsetX, offsetY) for em → band index
    int    shapeLocX; // band texture X of this shape's data block
    int    shapeLocY;
    int    bandMaxX;
    int    bandMaxY;
    float4 color;     // RGBA tint
};

// ---- Push constants (matching SlugPush in SlugHandler.cs) ----
struct SlugPush
{
    GlyphDrawData* draws;    // GPU pointer to instance buffer
    ImageHandle curveTexture;
    ImageHandle bandTexture;
    float4x4    projection;
};

[[vk::push_constant]]
uniform ConstantBuffer<SlugPush, ScalarDataLayout> push;

// ---- Vertex / fragment interpolants ----
struct VSOut
{
    float2 emCoord    : EM_COORD;
    int    instanceId : INSTANCE_ID;
    float4 position   : SV_Position;
};

struct FSIn
{
    float2 emCoord    : EM_COORD;
    int    instanceId : INSTANCE_ID;
};

// Lookup table mapping vertex index 0-5 to quad corner (0=min, 1=max) for x and y.
// Two counter-clockwise triangles: (0,0)→(1,0)→(0,1) and (1,0)→(1,1)→(0,1).
static const int2 kCorners[6] = {
    int2(0, 0), int2(1, 0), int2(0, 1),
    int2(1, 0), int2(1, 1), int2(0, 1)
};

[shader("vertex")]
VSOut vertex(int vid : SV_VertexID, int iid : SV_InstanceID)
{
    GlyphDrawData d = push.draws[iid];

    // Select corner (0=min, 1=max) for this vertex.
    float2 corner  = float2(kCorners[vid]);
    float2 screenPos = lerp(d.minPos, d.maxPos, corner);
    float2 emCoord   = lerp(d.minEm,  d.maxEm,  corner);

    VSOut o;
    o.position   = mul(push.projection, float4(screenPos, 0.0, 1.0));
    o.emCoord    = emCoord;
    o.instanceId = iid;
    return o;
}

// BlendUI: standard UI alpha blending — src.rgb*src.a + dst.rgb*(1−src.a).
// RGBA8 attachment format matches the main surface colour target.
[shader("fragment"), Attachment(AttachmentFormat.RGBA8), BlendUI]
float4 fragment(FSIn input) : SV_Target
{
    GlyphDrawData d = push.draws[input.instanceId];

    float coverage = SlugRender(
        input.emCoord,
        d.banding,
        int2(d.shapeLocX, d.shapeLocY),
        int2(d.bandMaxX,  d.bandMaxY),
        push.curveTexture,
        push.bandTexture);

    // Premultiplied-alpha output: the alpha channel carries the AA mask for blending.
    return d.color * coverage;
}
