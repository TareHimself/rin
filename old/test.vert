#version 450
#extension GL_EXT_buffer_reference : require
#extension GL_EXT_scalar_block_layout : require
vec2 applyTransform3(in vec2 pos, in mat3 projection)
{
 return(projection * vec3(pos, 1.0)).xy;
}
vec2 applyTransform4(in vec2 pos, in mat4 projection)
{
 return(projection * vec4(pos, 0.0, 1.0)).xy;
}
void extentToPoints(in vec4 extent, out vec2 tl, out vec2 tr, out vec2 bl, out vec2 br)
{
 vec2 p1 = extent.xy;
 vec2 p2 = extent.xy + extent.zw;
 tl = p1;
 br = p2;
 tr = vec2(br.x, tl.y);
 bl = vec2(tl.x, br.y);
}
float mapRangeUnClamped(in float value, in float fromMin, in float fromMax, in float toMin, in float toMax)
{
 float normalizedPosition = (value - fromMin) / (fromMax - fromMin);
 return mix(toMin, toMax, normalizedPosition);
}
vec2 normalizePoint(in vec4 viewport, in vec2 point)
{
 return vec2(mapRangeUnClamped(point.x, 0.0, viewport.z, - 1.0, 1.0), mapRangeUnClamped(point.y, 0.0, viewport.w, - 1.0, 1.0));
}
const vec2 vertices[] = { vec2(- 0.5), vec2(0.5, - 0.5), vec2(0.5), vec2(- 0.5), vec2(0.5), vec2(- 0.5, 0.5) };
void generateVertex(in vec4 viewport, in vec4 extent, in int index, out vec4 location, out vec2 uv)
{
 vec2 screenRes = viewport.zw;
 vec2 normPt1 = normalizePoint(viewport, extent.xy);
 vec2 normPt2 = normalizePoint(viewport, extent.xy + extent.zw);
 vec2 size = normPt2 - normPt1;
 vec2 midpoint = normPt1 + (size / 2);
 vec4 vLoc = vec4(midpoint + (size * vertices[index]), 0, 0);
 location = vec4(vLoc.xy, 0, 1);
 uv = vertices[index] + 0.5;
}
vec2 doProjectionAndTransformation(in vec2 pos, in mat4 projection, in mat3 transform)
{
 return applyTransform4(applyTransform3(pos, transform), projection);
}
vec2 undoProjectionAndTransformation(in vec2 pos, in mat4 projection, in mat3 transform)
{
 return applyTransform3(applyTransform4(pos, inverse(projection)), inverse(transform));
}
void generateRectVertex(in vec2 size, in mat4 projection, in mat3 transform, in int index, out vec4 location, out vec2 uv)
{
 vec4 extent = vec4(0.0, 0.0, size);
 vec2 tl;
 vec2 tr;
 vec2 bl;
 vec2 br;
 extentToPoints(extent, tl, tr, bl, br);
 vec2 vertex[] = { tl, tr, br, tl, br, bl };
 vec2 finalVert = doProjectionAndTransformation(vertex[index], projection, transform);
 location = vec4(finalVert, 0, 1);
 vec2 uvs[] = { vec2(0.0), vec2(1.0, 0.0), vec2(1.0), vec2(0.0), vec2(1.0), vec2(1.0, 0.0) };
 uv = vertex[index] / size;
}
bool shouldDiscard(in vec4 viewport, in vec4 clip, in vec2 pixel)
{
 vec4 clip_ss = vec4(normalizePoint(viewport, clip.xy), normalizePoint(viewport, clip.xy + clip.zw));
 vec2 pixel_ss = normalizePoint(viewport, pixel);
 return pixel_ss.x > clip_ss.z || pixel_ss.x < clip_ss.x || pixel_ss.y < clip_ss.y || pixel_ss.y > clip_ss.w;
}
struct Result {
 float dist;
 float side;
};
Result udSegment(in vec2 p, in vec2 a, in vec2 b)
{
 Result res;
 vec2 ba = b - a;
 vec2 pa = p - a;
 float h = clamp(dot(pa, ba), 0.0, 1.0);
 res.dist = length(pa - h * ba);
 res.side = sign( (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x));
 return res;
}
float sdfConvex4(in vec2 point, in vec2[4] points)
{
 Result r1 = udSegment(point, points[0], points[1]);
 Result r2 = udSegment(point, points[1], points[2]);
 Result r3 = udSegment(point, points[2], points[3]);
 Result r4 = udSegment(point, points[3], points[0]);
 float d = min(r1.dist, r2.dist);
 d = min(d, r3.dist);
 d = min(d, r4.dist);
 return d * sign(r1.side + r2.side + r3.side + r4.side + 4.0 - 0.5);
}
float roundedBoxSDF(in vec2 center, in vec2 size, in vec4 radius)
{
 radius.xy = (center.x > 0.0) ? radius.xy : radius.zw;
 radius.x = (center.y > 0.0) ? radius.x : radius.y;
 vec2 q = abs(center);
 return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - radius.x;
}
vec4 applyBorderRadius(in vec2 fragPosition, in vec4 color, in vec4 radius, in vec2 size, in mat3 transform)
{
 vec2 transformedFrag = applyTransform3(fragPosition, inverse(transform));
 float edgeSoftness = 2.0;
 vec2 halfSize = size / 2.0;
 float distance = roundedBoxSDF(transformedFrag - halfSize, halfSize, radius);
 float smoothedAlpha = smoothstep(0.0, edgeSoftness, distance);
 return mix(color, vec4(color.xyz, 0.0), smoothedAlpha);
}
struct QuadRenderInfo {
 int textureId;
 int samplerId;
 vec4 color;
 vec4 borderRadius;
 vec2 size;
 mat3 transform;
};
layout(set = 0, binding = 0, scalar) uniform _block_batch_info {
 float time;
 vec4 viewport;
 mat4 projection;
 QuadRenderInfo quads[1024];
} batch_info;
layout(location = 0) out vec2 oUV;
layout(location = 1) out int oQuadIndex;
void main()
{
 int index = gl_VertexIndex;
 int vertexIndex = int(mod(index, 6));
 int quadIndex = int(floor(index / 6));
 QuadRenderInfo renderInfo = batch_info.quads[quadIndex];
 vec4 extent = vec4(0.0, 0.0, renderInfo.size);
 vec2 tl;
 vec2 tr;
 vec2 bl;
 vec2 br;
 extentToPoints(extent, tl, tr, bl, br);
 vec2 vertex[] = { tl, tr, br, tl, br, bl };
 vec2 finalVert = doProjectionAndTransformation(vertex[vertexIndex], batch_info.projection, renderInfo.transform);
 gl_Position = vec4(finalVert, 0, 1);
 vec2 uvs[] = { vec2(0.0), vec2(1.0, 0.0), vec2(1.0), vec2(0.0), vec2(1.0), vec2(1.0, 0.0) };
 oUV = vertex[vertexIndex] / size;
 oQuadIndex = quadIndex;
}

