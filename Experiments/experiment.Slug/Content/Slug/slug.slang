// slug.slang — SLUG GPU vector rendering.
//
// This is a from-scratch Slang port of Eric Lengyel's reference implementation
// (https://github.com/EricLengyel/Slug — SlugPixelShader.hlsl / SlugVertexShader.hlsl,
// MIT/Apache-2.0), following the algorithm described in "GPU-Accelerated Path Rendering"
// (JCGT vol 6 no 2, 2017, https://jcgt.org/published/0006/02/02/). The patent has been
// dedicated to the public domain.
//
// ALGORITHM
// =========
// For each pixel, SLUG casts a leftward horizontal ray and a downward vertical ray against
// the shape's quadratic Bezier outline and counts signed crossings (nonzero winding rule).
// To avoid testing every curve for every pixel, each shape's bounding box is pre-divided into
// horizontal and vertical "bands" (see ShapeBandPacker.cs); a pixel only walks the curves
// overlapping its own band, sorted so the shader can break out early once the remaining
// curves in the band can no longer affect the sample.
//
// TEXTURE LAYOUT
// ==============
// Curve texture (RGBA32F, 4096 wide): 2 texels per curve —
//   texel 0 = (p0.x, p0.y, p1.x, p1.y), texel 1 = (p2.x, p2.y, 0, 0)
// Band texture (RG32F, 4096 wide), one block per shape starting at shapeLoc:
//   [0 .. bandMax.y]                H band headers: (count, listOffset)
//   [bandMax.y+1 .. +bandMax.x+1]   V band headers: (count, listOffset)
//   [listOffset ..]                 curve index entries: (curveTexelX, curveTexelY)
// (Reference reads 16-bit int/uint channels; Rin has no integer texture format, so both
// textures here use float32 channels — counts/offsets/coordinates are small integers and are
// exactly representable.)

#include "Shaders/Core/Attributes/blending.slang"
#include "Shaders/Core/images.slang"

static const int kLogBandWidth = 12; // 2^12 = 4096
static const int kBandWidth    = 4096;

// Translate an offset (texels from a shape's band-block origin) into absolute band-texture
// coordinates, wrapping rows at kBandWidth. Ported from the reference CalcBandLoc.
int2 CalcBandLoc(int2 shapeLoc, int offset)
{
    int absolute = shapeLoc.x + offset;
    return int2(absolute & (kBandWidth - 1), shapeLoc.y + (absolute >> kLogBandWidth));
}

// Determine which of a quadratic's two roots are genuine ray crossings, from the sign pattern
// of the three control points' coordinates along the tested axis. Ported verbatim from the
// reference CalcRootCode (the 0x2E74 table is derived in the original Slug source comments);
// bit 0 flags the first root (t1), bit 8 flags the second root (t2).
uint CalcRootCode(float y1, float y2, float y3)
{
    uint i1 = asuint(y1) >> 31u;
    uint i2 = asuint(y2) >> 30u;
    uint i3 = asuint(y3) >> 29u;
    uint shift = (i2 & 2u) | (i1 & ~2u);
    shift = (i3 & 4u) | (shift & ~4u);
    return (0x2E74u >> shift) & 0x0101u;
}

// Solve for the x-coordinates where the curve (p1,p2,p3, already sample-relative) crosses
// y = 0. Ported from the reference SolveHorizPoly.
float2 SolveHorizPoly(float2 p1, float2 p2, float2 p3)
{
    float2 a = p1 - p2 * 2.0 + p3;
    float2 b = p1 - p2;
    float ra = 1.0 / a.y;
    float rb = 0.5 / b.y;

    float d = sqrt(max(b.y * b.y - a.y * p1.y, 0.0));
    float t1 = (b.y - d) * ra;
    float t2 = (b.y + d) * ra;

    // Near-linear fallback: solve the degree-1 remainder -2b*t + c = 0 instead.
    if (abs(a.y) < (1.0 / 65536.0)) { t1 = p1.y * rb; t2 = t1; }

    return float2((a.x * t1 - b.x * 2.0) * t1 + p1.x, (a.x * t2 - b.x * 2.0) * t2 + p1.x);
}

// Solve for the y-coordinates where the curve crosses x = 0. Ported from SolveVertPoly.
float2 SolveVertPoly(float2 p1, float2 p2, float2 p3)
{
    float2 a = p1 - p2 * 2.0 + p3;
    float2 b = p1 - p2;
    float ra = 1.0 / a.x;
    float rb = 0.5 / b.x;

    float d = sqrt(max(b.x * b.x - a.x * p1.x, 0.0));
    float t1 = (b.x - d) * ra;
    float t2 = (b.x + d) * ra;

    if (abs(a.x) < (1.0 / 65536.0)) { t1 = p1.x * rb; t2 = t1; }

    return float2((a.y * t1 - b.y * 2.0) * t1 + p1.y, (a.y * t2 - b.y * 2.0) * t2 + p1.y);
}

// Combine the horizontal- and vertical-ray coverage estimates into one antialiased coverage
// value, weighting each by how close its nearest crossing was to the pixel center. Ported from
// the reference CalcCoverage (nonzero fill rule only — the reference's even-odd/weight-boost
// compile-time options are omitted here).
float CalcCoverage(float xcov, float ycov, float xwgt, float ywgt)
{
    float coverage = max(abs(xcov * xwgt + ycov * ywgt) / max(xwgt + ywgt, 1.0 / 65536.0),
                          min(abs(xcov), abs(ycov)));
    return saturate(coverage);
}

// Per-pixel SLUG evaluation for one shape instance.
//   emCoord:  this pixel's em-space coordinate (interpolated across the instance's quad)
//   banding:  (scaleX, scaleY, offsetX, offsetY) mapping emCoord -> band index
//   shapeLoc: (x, y) of this shape's band-texture data block origin
//   bandMax:  (bandMaxX, bandMaxY) = band counts minus one, for clamping
float SlugRender(
    float2      emCoord,
    float4      banding,
    int2        shapeLoc,
    int2        bandMax,
    ImageHandle curveTexture,
    ImageHandle bandTexture)
{
    float2 pixelsPerEm = 1.0 / max(fwidth(emCoord), float2(1e-6));
    int2 bandIndex = clamp(int2(emCoord * banding.xy + banding.zw), int2(0, 0), bandMax);

    float xcov = 0.0;
    float xwgt = 0.0;

    // ---- Horizontal band: leftward-ray crossings, walked in descending max-x order ----
    {
        float2 header = bandTexture.TexelLoad(int2(shapeLoc.x + bandIndex.y, shapeLoc.y)).xy;
        int count  = int(header.x);
        int2 listLoc = CalcBandLoc(shapeLoc, int(header.y));

        for (int i = 0; i < count; i++)
        {
            int2 curveLoc = int2(bandTexture.TexelLoad(int2(listLoc.x + i, listLoc.y)).xy);

            float4 c01 = curveTexture.TexelLoad(curveLoc) - float4(emCoord, emCoord);
            float2 p3  = curveTexture.TexelLoad(curveLoc + int2(1, 0)).xy - emCoord;
            float2 p1 = c01.xy;
            float2 p2 = c01.zw;

            if (max(max(p1.x, p2.x), p3.x) * pixelsPerEm.x < -0.5) break;

            uint code = CalcRootCode(p1.y, p2.y, p3.y);
            if (code != 0u)
            {
                float2 r = SolveHorizPoly(p1, p2, p3) * pixelsPerEm.x;
                if ((code & 1u) != 0u)
                {
                    xcov += saturate(r.x + 0.5);
                    xwgt = max(xwgt, saturate(1.0 - abs(r.x) * 2.0));
                }
                if (code > 1u)
                {
                    xcov -= saturate(r.y + 0.5);
                    xwgt = max(xwgt, saturate(1.0 - abs(r.y) * 2.0));
                }
            }
        }
    }

    float ycov = 0.0;
    float ywgt = 0.0;

    // ---- Vertical band: downward-ray crossings, walked in descending max-y order ----
    {
        int2 headerLoc = int2(shapeLoc.x + bandMax.y + 1 + bandIndex.x, shapeLoc.y);
        float2 header = bandTexture.TexelLoad(headerLoc).xy;
        int count  = int(header.x);
        int2 listLoc = CalcBandLoc(shapeLoc, int(header.y));

        for (int i = 0; i < count; i++)
        {
            int2 curveLoc = int2(bandTexture.TexelLoad(int2(listLoc.x + i, listLoc.y)).xy);

            float4 c01 = curveTexture.TexelLoad(curveLoc) - float4(emCoord, emCoord);
            float2 p3  = curveTexture.TexelLoad(curveLoc + int2(1, 0)).xy - emCoord;
            float2 p1 = c01.xy;
            float2 p2 = c01.zw;

            if (max(max(p1.y, p2.y), p3.y) * pixelsPerEm.y < -0.5) break;

            uint code = CalcRootCode(p1.x, p2.x, p3.x);
            if (code != 0u)
            {
                float2 r = SolveVertPoly(p1, p2, p3) * pixelsPerEm.y;
                if ((code & 1u) != 0u)
                {
                    ycov -= saturate(r.x + 0.5);
                    ywgt = max(ywgt, saturate(1.0 - abs(r.x) * 2.0));
                }
                if (code > 1u)
                {
                    ycov += saturate(r.y + 0.5);
                    ywgt = max(ywgt, saturate(1.0 - abs(r.y) * 2.0));
                }
            }
        }
    }

    return CalcCoverage(xcov, ycov, xwgt, ywgt);
}

// ---- Per-instance draw data, matching SlugInstanceData in C# exactly ----
struct SlugInstanceData
{
    float2 minPos;
    float2 maxPos;
    float2 minEm;
    float2 maxEm;
    float4 banding;
    int    shapeLocX;
    int    shapeLocY;
    int    bandMaxX;
    int    bandMaxY;
    float4 color;
};

// ---- Push constants, matching SlugPush in SlugDrawCommand.cs ----
struct SlugPush
{
    SlugInstanceData* instances;
    ImageHandle curveTexture;
    ImageHandle bandTexture;
    float4x4    projection;
};

[[vk::push_constant]]
uniform ConstantBuffer<SlugPush, ScalarDataLayout> push;

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

// Two CCW triangles covering the instance's screen-space quad; vertex index -> corner.
static const int2 kCorners[6] = {
    int2(0, 0), int2(1, 0), int2(0, 1),
    int2(1, 0), int2(1, 1), int2(0, 1)
};

[shader("vertex")]
VSOut vertex(int vid : SV_VertexID, int iid : SV_InstanceID)
{
    SlugInstanceData d = push.instances[iid];
    float2 corner = float2(kCorners[vid]);

    VSOut o;
    o.position   = mul(push.projection, float4(lerp(d.minPos, d.maxPos, corner), 0.0, 1.0));
    o.emCoord    = lerp(d.minEm, d.maxEm, corner);
    o.instanceId = iid;
    return o;
}

[shader("fragment"), Attachment(AttachmentFormat.RGBA32), BlendUI]
float4 fragment(FSIn input) : SV_Target
{
    SlugInstanceData d = push.instances[input.instanceId];

    float coverage = SlugRender(
        input.emCoord,
        d.banding,
        int2(d.shapeLocX, d.shapeLocY),
        int2(d.bandMaxX, d.bandMaxY),
        push.curveTexture,
        push.bandTexture);

    return d.color * coverage;
}
