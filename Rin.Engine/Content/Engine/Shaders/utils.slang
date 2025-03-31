#include "Engine/Shaders/operators.slang"

float3 rgb2lin(float3 rgb) { // sRGB to linear approximation
    return pow(rgb, float3(2.2));
}

float3 lin2rgb(float3 lin) { // linear to sRGB approximation
    return pow(lin, float3(1.0 / 2.2));
}

// https://skia.googlesource.com/skia/+/f95647aa03ca/tests/sksl/intrinsics/Inverse.hlsl?autodive=0%2F%2F%2F%2F%2F%2F%2F%2F%2F%2F
// Returns the determinant of a 2x2 matrix.
float spvDet2x2(float a1, float a2, float b1, float b2)
{
    return a1 * b2 - b1 * a2;
}
// Returns the inverse of a matrix, by using the algorithm of calculating the classical
// adjoint and dividing by the determinant. The contents of the matrix are changed.
float3x3 inverse(float3x3 s)
{
    float3x3 m = s;
    float3x3 adj; // The adjoint matrix (inverse after dividing by determinant)
    // Create the transpose of the cofactors, as the classical adjoint of the matrix.
    adj[0][0] = spvDet2x2(m[1][1], m[1][2], m[2][1], m[2][2]);
    adj[0][1] = -spvDet2x2(m[0][1], m[0][2], m[2][1], m[2][2]);
    adj[0][2] = spvDet2x2(m[0][1], m[0][2], m[1][1], m[1][2]);
    adj[1][0] = -spvDet2x2(m[1][0], m[1][2], m[2][0], m[2][2]);
    adj[1][1] = spvDet2x2(m[0][0], m[0][2], m[2][0], m[2][2]);
    adj[1][2] = -spvDet2x2(m[0][0], m[0][2], m[1][0], m[1][2]);
    adj[2][0] = spvDet2x2(m[1][0], m[1][1], m[2][0], m[2][1]);
    adj[2][1] = -spvDet2x2(m[0][0], m[0][1], m[2][0], m[2][1]);
    adj[2][2] = spvDet2x2(m[0][0], m[0][1], m[1][0], m[1][1]);
    // Calculate the determinant as a combination of the cofactors of the first row.
    float det = (adj[0][0] * m[0][0]) + (adj[0][1] * m[1][0]) + (adj[0][2] * m[2][0]);
    // Divide the classical adjoint matrix by the determinant.
    // If determinant is zero, matrix is not invertable, so leave it unchanged.
    return (det != 0.0f) ? (adj * (1.0f / det)) : m;
}

// https://gist.github.com/mattatz/86fff4b32d198d0928d0fa4ff32cf6fa
float4x4 inverse(float4x4 m)
{
    float n11 = m[0][0], n12 = m[1][0], n13 = m[2][0], n14 = m[3][0];
    float n21 = m[0][1], n22 = m[1][1], n23 = m[2][1], n24 = m[3][1];
    float n31 = m[0][2], n32 = m[1][2], n33 = m[2][2], n34 = m[3][2];
    float n41 = m[0][3], n42 = m[1][3], n43 = m[2][3], n44 = m[3][3];

    float t11 = n23 * n34 * n42 - n24 * n33 * n42 + n24 * n32 * n43 - n22 * n34 * n43 - n23 * n32 * n44 + n22 * n33 * n44;
    float t12 = n14 * n33 * n42 - n13 * n34 * n42 - n14 * n32 * n43 + n12 * n34 * n43 + n13 * n32 * n44 - n12 * n33 * n44;
    float t13 = n13 * n24 * n42 - n14 * n23 * n42 + n14 * n22 * n43 - n12 * n24 * n43 - n13 * n22 * n44 + n12 * n23 * n44;
    float t14 = n14 * n23 * n32 - n13 * n24 * n32 - n14 * n22 * n33 + n12 * n24 * n33 + n13 * n22 * n34 - n12 * n23 * n34;

    float det = n11 * t11 + n21 * t12 + n31 * t13 + n41 * t14;
    float idet = 1.0f / det;

    float4x4 ret;

    ret[0][0] = t11 * idet;
    ret[0][1] = (n24 * n33 * n41 - n23 * n34 * n41 - n24 * n31 * n43 + n21 * n34 * n43 + n23 * n31 * n44 - n21 * n33 * n44) * idet;
    ret[0][2] = (n22 * n34 * n41 - n24 * n32 * n41 + n24 * n31 * n42 - n21 * n34 * n42 - n22 * n31 * n44 + n21 * n32 * n44) * idet;
    ret[0][3] = (n23 * n32 * n41 - n22 * n33 * n41 - n23 * n31 * n42 + n21 * n33 * n42 + n22 * n31 * n43 - n21 * n32 * n43) * idet;

    ret[1][0] = t12 * idet;
    ret[1][1] = (n13 * n34 * n41 - n14 * n33 * n41 + n14 * n31 * n43 - n11 * n34 * n43 - n13 * n31 * n44 + n11 * n33 * n44) * idet;
    ret[1][2] = (n14 * n32 * n41 - n12 * n34 * n41 - n14 * n31 * n42 + n11 * n34 * n42 + n12 * n31 * n44 - n11 * n32 * n44) * idet;
    ret[1][3] = (n12 * n33 * n41 - n13 * n32 * n41 + n13 * n31 * n42 - n11 * n33 * n42 - n12 * n31 * n43 + n11 * n32 * n43) * idet;

    ret[2][0] = t13 * idet;
    ret[2][1] = (n14 * n23 * n41 - n13 * n24 * n41 - n14 * n21 * n43 + n11 * n24 * n43 + n13 * n21 * n44 - n11 * n23 * n44) * idet;
    ret[2][2] = (n12 * n24 * n41 - n14 * n22 * n41 + n14 * n21 * n42 - n11 * n24 * n42 - n12 * n21 * n44 + n11 * n22 * n44) * idet;
    ret[2][3] = (n13 * n22 * n41 - n12 * n23 * n41 - n13 * n21 * n42 + n11 * n23 * n42 + n12 * n21 * n43 - n11 * n22 * n43) * idet;

    ret[3][0] = t14 * idet;
    ret[3][1] = (n13 * n24 * n31 - n14 * n23 * n31 + n14 * n21 * n33 - n11 * n24 * n33 - n13 * n21 * n34 + n11 * n23 * n34) * idet;
    ret[3][2] = (n14 * n22 * n31 - n12 * n24 * n31 - n14 * n21 * n32 + n11 * n24 * n32 + n12 * n21 * n34 - n11 * n22 * n34) * idet;
    ret[3][3] = (n12 * n23 * n31 - n13 * n22 * n31 + n13 * n21 * n32 - n11 * n23 * n32 - n12 * n21 * n33 + n11 * n22 * n33) * idet;

    return ret;
}

float2 applyTransform3(float2 pos, float3x3 projection)
{
    return (projection * float3(pos, 1.0)).xy;
}

float2 applyTransform4(float2 pos, float4x4 projection)
{
    return (projection * float4(pos, 0.0, 1.0)).xy;
}

void extentToPoints(float4 extent, out float2 tl, out float2 tr, out float2 bl, out float2 br)
{
    float2 p1 = extent.xy;
    float2 p2 = extent.xy + extent.zw;
    tl = p1;
    br = p2;
    tr = float2(br.x, tl.y);
    bl = float2(tl.x, br.y);
}

// T mapRangeUnClamped<T : __BuiltinFloatingPointType>(T value, T fromMin, T fromMax, T toMin, T toMax)
// {

//     // Calculate the normalized position of the value in the input range
//     var normalizedPosition = (value - fromMin) / (fromMax - fromMin);

//     // Map the normalized position to the output range [toMin, toMax]
//     return lerp(toMin, toMax, normalizedPosition);
// }

float mapRangeUnClamped(float value, float fromMin, float fromMax, float toMin, float toMax)
{

    // Calculate the normalized position of the value in the input range
    var normalizedPosition = (value - fromMin) / (fromMax - fromMin);

    // Map the normalized position to the output range [toMin, toMax]
    return lerp(toMin, toMax, normalizedPosition);
}

float2 mapRangeUnClamped(float2 value, float2 fromMin, float2 fromMax, float2 toMin, float2 toMax)
{

    // Calculate the normalized position of the value in the input range
    var normalizedPosition = (value - fromMin) / (fromMax - fromMin);

    // Map the normalized position to the output range [toMin, toMax]
    return lerp(toMin, toMax, normalizedPosition);
}

float3 mapRangeUnClamped(float3 value, float3 fromMin, float3 fromMax, float3 toMin, float3 toMax)
{

    // Calculate the normalized position of the value in the input range
    var normalizedPosition = (value - fromMin) / (fromMax - fromMin);

    // Map the normalized position to the output range [toMin, toMax]
    return lerp(toMin, toMax, normalizedPosition);
}

float4 mapRangeUnClamped(float4 value, float4 fromMin, float4 fromMax, float4 toMin, float4 toMax)
{

    // Calculate the normalized position of the value in the input range
    var normalizedPosition = (value - fromMin) / (fromMax - fromMin);

    // Map the normalized position to the output range [toMin, toMax]
    return lerp(toMin, toMax, normalizedPosition);
}


float2 normalizePoint(float4 viewport, float2 point)
{
    return float2(mapRangeUnClamped(point.x, 0.0, viewport.z, -1.0, 1.0), mapRangeUnClamped(point.y, 0.0, viewport.w, -1.0, 1.0));
}

const static float2 vertices[] = { float2(-0.5), float2(0.5, -0.5), float2(0.5), float2(-0.5), float2(0.5), float2(-0.5, 0.5) };

void generateVertex(float4 viewport, float4 extent, int index, out float4 location, out float2 uv)
{
    float2 screenRes = viewport.zw;
    float2 normPt1 = normalizePoint(viewport, extent.xy);
    float2 normPt2 = normalizePoint(viewport, extent.xy + extent.zw);
    float2 size = normPt2 - normPt1;
    float2 midpoint = normPt1 + (size / 2);

    // float2 vertex[] = { float2(-0.5), float2(0.5, -0.5), float2(0.5), float2(-0.5), float2(0.5), float2(-0.5, 0.5) };

    float4 vLoc = float4(midpoint + (size * vertices[index]), 0, 0);

    location = float4(vLoc.xy, 0, 1);

    uv = vertices[index] + 0.5;
}

float2 doProjectionAndTransformation(float2 pos, float4x4 projection, float3x3 transform)
{
    return applyTransform4(applyTransform3(pos, transform), projection);
}

float2 undoProjectionAndTransformation(float2 pos, float4x4 projection, float3x3 transform)
{
    return applyTransform3(applyTransform4(pos, inverse(projection)), inverse(transform));
}

void generateRectVertex(float2 size, float4x4 projection, float3x3 transform, int index, out float4 location, out float2 uv)
{
    float4 extent = float4(0.0, 0.0, size);
    float2 tl;
    float2 tr;
    float2 bl;
    float2 br;
    extentToPoints(extent, tl, tr, bl, br);

    float2 vertex[] = { tl, tr, br, tl, br, bl };

    float2 finalVert = doProjectionAndTransformation(vertex[index], projection, transform);

    location = float4(finalVert, 0, 1);

    float2 uvs[] = { float2(0.0), float2(1.0, 0.0), float2(1.0), float2(0.0), float2(1.0), float2(1.0, 0.0) };

    uv = vertex[index] / size;
}

void generateRectVertex(float2 size, float4x4 projection, float4x4 transform, int index, out float4 location, out float2 uv)
{
    float4 extent = float4(0.0, 0.0, size);
    float2 tl;
    float2 tr;
    float2 bl;
    float2 br;
    extentToPoints(extent, tl, tr, bl, br);

    float2 vertex[] = { tl, tr, br, tl, br, bl };

    float2 finalVert = projection * (transform * vertex[index]);

    location = float4(finalVert, 0, 1);

    float2 uvs[] = { float2(0.0), float2(1.0, 0.0), float2(1.0), float2(0.0), float2(1.0), float2(1.0, 0.0) };

    uv = vertex[index] / size;
}

void generateFullScreenVertex(int index, out float4 location, out float2 uv)
{
    float2 vertex[] = { float2(-1.0), float2(1.0, -1.0), float2(1.0), float2(-1.0), float2(1.0), float2(-1.0, 1.0) };

    location = float4(vertex[index], 0, 1);

    float2 uvs[] = { float2(0.0), float2(1.0, 0.0), float2(1.0), float2(0.0), float2(1.0), float2(1.0, 0.0) };

    uv = (vertex[index] + float2(1.0)) / float2(2.0);
}

bool shouldDiscard(float4 viewport, float4 clip, float2 pixel)
{

    float4 clip_ss = float4(normalizePoint(viewport, clip.xy), normalizePoint(viewport, clip.xy + clip.zw));
    float2 pixel_ss = normalizePoint(viewport, pixel);
    return pixel_ss.x > clip_ss.z || pixel_ss.x < clip_ss.x || pixel_ss.y < clip_ss.y || pixel_ss.y > clip_ss.w;
}

struct SegmentResult
{
    float dist; // Regular SDF distance
    float side; // Which side of the line segment the point is (-1,0,1)
};

// https://www.shadertoy.com/view/wdffWH
SegmentResult udSegment(float2 p, float2 a, float2 b)
{
    SegmentResult res;
    // All this is basically Inigo's regular line SDF function - but store it in 'dist' instead:
    float2 ba = b - a;
    float2 pa = p - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    res.dist = length(pa - h * ba);
    // Is the movement (a->b->p) a righthand turn? (cross product)
    res.side = sign((b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x));
    return res;
}

float sdfConvex4(float2 point, float2[4] points)
{

    var r1 = udSegment(point, points[0], points[1]);
    var r2 = udSegment(point, points[1], points[2]);
    var r3 = udSegment(point, points[2], points[3]);
    var r4 = udSegment(point, points[3], points[0]);

    float d = min(r1.dist, r2.dist);
    d = min(d, r3.dist);
    d = min(d, r4.dist);

    return d * sign(r1.side + r2.side + r3.side + r4.side + 4.0 - 0.5);
}

// https://www.shadertoy.com/view/fsdyzB
float roundedBoxSDF(float2 center, float2 size, float4 radius)
{
    radius.xy = (center.x > 0.0) ? radius.xy : radius.zw;
    radius.x = (center.y > 0.0) ? radius.x : radius.y;

    float2 q = abs(center) - size + radius.x;
    return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - radius.x;
}

// https://www.shadertoy.com/view/WtdSDs
float4 applyBorderRadius(float2 fragPosition, float4 color, float4 radius, float2 size, float3x3 transform)
{

    float2 transformedFrag = applyTransform3(fragPosition, inverse(transform));

    // How soft the edges should be (in pixels). Higher values could be used to simulate a drop shadow.
    float edgeSoftness = 1.0f;

    float2 halfSize = size / 2.0f;
    // Calculate distance to edge.
    float distance = roundedBoxSDF(transformedFrag - halfSize, halfSize, radius);

    // Smooth the result (free antialiasing).
    float smoothedAlpha = smoothstep(0.0f, edgeSoftness, distance);

    return lerp(color, float4(color.xyz, 0.0), smoothedAlpha);
}

float4 applyBorderRadius(float2 fragPosition, float4 color, float4 radius, float2 size, float4x4 transform)
{

    float2 transformedFrag = inverse(transform) * fragPosition;

    // How soft the edges should be (in pixels). Higher values could be used to simulate a drop shadow.
    float edgeSoftness = 1.0f;

    float2 halfSize = size / 2.0f;
    // Calculate distance to edge.
    float distance = roundedBoxSDF(transformedFrag - halfSize, halfSize, radius);

    // Smooth the result (free antialiasing).
    float smoothedAlpha = smoothstep(0.0f, edgeSoftness, distance);

    return lerp(color, float4(color.xyz, 0.0), smoothedAlpha);
}

float3 rgb2hsv(float3 c)
{
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
    float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

float3 hsv2rgb(float3 c)
{
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}
