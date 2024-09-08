float2 applyTransform3(float2 pos, mat3 projection){
    return (projection * float3(pos, 1.0)).xy;
}

float2 applyTransform4(float2 pos, mat4 projection){
    return (projection * float4(pos, 0.0, 1.0)).xy;
}

void extentToPoints(float4 extent, out float2 tl, out float2 tr, out float2 bl, out float2 br){
    float2 p1 = extent.xy;
    float2 p2 = extent.xy + extent.zw;
    tl = p1;
    br = p2;
    tr = float2(br.x, tl.y);
    bl = float2(tl.x, br.y);
}

float mapRangeUnClamped(float value, float fromMin, float fromMax, float toMin, float toMax) {

    // Calculate the normalized position of the value in the input range
    float normalizedPosition = (value - fromMin) / (fromMax - fromMin);

    // Map the normalized position to the output range [toMin, toMax]
    return mix(toMin, toMax, normalizedPosition);
}

float2 normalizePoint(float4 viewport, float2 point){
    return float2(mapRangeUnClamped(point.x, 0.0, viewport.z, -1.0, 1.0), mapRangeUnClamped(point.y, 0.0, viewport.w, -1.0, 1.0));
}

const float2 vertices[] = { float2(-0.5), float2(0.5, -0.5), float2(0.5), float2(-0.5), float2(0.5), float2(-0.5, 0.5) };

void generateVertex(float4 viewport, float4 extent, int index, out float4 location, out float2 uv){
    float2 screenRes = viewport.zw;
    float2 normPt1 = normalizePoint(viewport, extent.xy);
    float2 normPt2 = normalizePoint(viewport, extent.xy + extent.zw);
    float2 size = normPt2 - normPt1;
    float2 midpoint = normPt1 + (size / 2);

    //float2 vertex[] = { float2(-0.5), float2(0.5, -0.5), float2(0.5), float2(-0.5), float2(0.5), float2(-0.5, 0.5) };

    float4 vLoc = float4(midpoint + (size * vertices[index]), 0, 0);

    location = float4(vLoc.xy, 0, 1);

    uv = vertices[index] + 0.5;
}


float2 doProjectionAndTransformation(float2 pos, mat4 projection, mat3 transform){
    return applyTransform4(applyTransform3(pos, transform), projection);
}

float2 undoProjectionAndTransformation(float2 pos, mat4 projection, mat3 transform){
    return applyTransform3(applyTransform4(pos, inverse(projection)), inverse(transform));
}

void generateRectVertex(float2 size, mat4 projection, mat3 transform, int index, out float4 location, out float2 uv){
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

bool shouldDiscard(float4 viewport, float4 clip, float2 pixel){

    float4 clip_ss = float4(normalizePoint(viewport, clip.xy), normalizePoint(viewport, clip.xy + clip.zw));
    float2 pixel_ss = normalizePoint(viewport, pixel);
    return pixel_ss.x > clip_ss.z || pixel_ss.x < clip_ss.x || pixel_ss.y < clip_ss.y || pixel_ss.y > clip_ss.w;
}

struct Result{
    float dist;//Regular SDF distance
    float side;//Which side of the line segment the point is (-1,0,1)
};

// https://www.shadertoy.com/view/wdffWH
Result udSegment(float2 p, float2 a, float2 b)
{
    Result res;
    //All this is basically Inigo's regular line SDF function - but store it in 'dist' instead: 
    float2 ba = b-a;
    float2 pa = p-a;
    float h = clamp(dot(pa, ba)/dot(ba, ba), 0.0, 1.0);
    res.dist = length(pa-h*ba);
    //Is the movement (a->b->p) a righthand turn? (cross product)
    res.side = sign((b.x - a.x)*(p.y - a.y) - (b.y - a.y)*(p.x - a.x));
    return res;
}

float sdfConvex4(float2 point, float2[4] points){

    Result r1 = udSegment(point, points[0], points[1]);
    Result r2 = udSegment(point, points[1], points[2]);
    Result r3 = udSegment(point, points[2], points[3]);
    Result r4 = udSegment(point, points[3], points[0]);

    float d = min(r1.dist, r2.dist);
    d = min(d, r3.dist);
    d = min(d, r4.dist);

    return d * sign(r1.side + r2.side + r3.side + r4.side + 4.0 - 0.5);
}

// https://www.shadertoy.com/view/fsdyzB
float roundedBoxSDF(float2 center, float2 size, float4 radius)
{
    radius.xy = (center.x > 0.0) ? radius.xy : radius.zw;
    radius.x  = (center.y > 0.0) ? radius.x  : radius.y;

    float2 q = abs(center)-size+radius.x;
    return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - radius.x;
}

// https://www.shadertoy.com/view/WtdSDs
float4 applyBorderRadius(float2 fragPosition, float4 color, float4 radius, float2 size, mat3 transform){

    float2 transformedFrag = applyTransform3(fragPosition, inverse(transform));

    // How soft the edges should be (in pixels). Higher values could be used to simulate a drop shadow.
    float edgeSoftness = 2.0f;

    float2 halfSize = size / 2.0f;
    // Calculate distance to edge.   
    float distance = roundedBoxSDF(transformedFrag - halfSize, halfSize, radius);

    // Smooth the result (free antialiasing).
    float smoothedAlpha =  smoothstep(0.0f, edgeSoftness, distance);


    return mix(color, float4(color.xyz, 0.0), smoothedAlpha);
}
