vec2 applyTransform3(vec2 pos, mat3 projection){
    return (projection * vec3(pos, 1.0)).xy;
}

vec2 applyTransform4(vec2 pos, mat4 projection){
    return (projection * vec4(pos, 0.0, 1.0)).xy;
}

void extentToPoints(vec4 extent, out vec2 tl, out vec2 tr, out vec2 bl, out vec2 br){
    vec2 p1 = extent.xy;
    vec2 p2 = extent.xy + extent.zw;
    tl = p1;
    br = p2;
    tr = vec2(br.x, tl.y);
    bl = vec2(tl.x, br.y);
}

float mapRangeUnClamped(float value, float fromMin, float fromMax, float toMin, float toMax) {

    // Calculate the normalized position of the value in the input range
    float normalizedPosition = (value - fromMin) / (fromMax - fromMin);

    // Map the normalized position to the output range [toMin, toMax]
    return mix(toMin, toMax, normalizedPosition);
}

vec2 normalizePoint(vec4 viewport, vec2 point){
    return vec2(mapRangeUnClamped(point.x, 0.0, viewport.z, -1.0, 1.0), mapRangeUnClamped(point.y, 0.0, viewport.w, -1.0, 1.0));
}

const vec2 vertices[] = { vec2(-0.5), vec2(0.5, -0.5), vec2(0.5), vec2(-0.5), vec2(0.5), vec2(-0.5, 0.5) };

void generateVertex(vec4 viewport, vec4 extent, int index, out vec4 location, out vec2 uv){
    vec2 screenRes = viewport.zw;
    vec2 normPt1 = normalizePoint(viewport, extent.xy);
    vec2 normPt2 = normalizePoint(viewport, extent.xy + extent.zw);
    vec2 size = normPt2 - normPt1;
    vec2 midpoint = normPt1 + (size / 2);

    //vec2 vertex[] = { vec2(-0.5), vec2(0.5, -0.5), vec2(0.5), vec2(-0.5), vec2(0.5), vec2(-0.5, 0.5) };

    vec4 vLoc = vec4(midpoint + (size * vertices[index]), 0, 0);

    location = vec4(vLoc.xy, 0, 1);

    uv = vertices[index] + 0.5;
}


vec2 doProjectionAndTransformation(vec2 pos, mat4 projection, mat3 transform){
    return applyTransform4(applyTransform3(pos, transform), projection);
}

vec2 undoProjectionAndTransformation(vec2 pos, mat4 projection, mat3 transform){
    return applyTransform3(applyTransform4(pos, inverse(projection)), inverse(transform));
}

void generateRectVertex(vec2 size, mat4 projection, mat3 transform, int index, out vec4 location, out vec2 uv){
    vec4 extent = vec4(0.0, 0.0, size);
    vec2 tl, tr, bl, br;
    extentToPoints(extent, tl, tr, bl, br);

    vec2 vertex[] = { tl, tr, br, tl, br, bl };

    vec2 finalVert = doProjectionAndTransformation(vertex[index], projection, transform);

    location = vec4(finalVert, 0, 1);

    vec2 uvs[] = { vec2(0.0), vec2(1.0, 0.0), vec2(1.0), vec2(0.0), vec2(1.0), vec2(1.0, 0.0) };

    uv = vertex[index] / size;
}

bool shouldDiscard(vec4 viewport, vec4 clip, vec2 pixel){

    vec4 clip_ss = vec4(normalizePoint(viewport, clip.xy), normalizePoint(viewport, clip.xy + clip.zw));
    vec2 pixel_ss = normalizePoint(viewport, pixel);
    return pixel_ss.x > clip_ss.z || pixel_ss.x < clip_ss.x || pixel_ss.y < clip_ss.y || pixel_ss.y > clip_ss.w;
}

struct Result{
    float dist;//Regular SDF distance
    float side;//Which side of the line segment the point is (-1,0,1)
};

// https://www.shadertoy.com/view/wdffWH
Result udSegment(vec2 p, vec2 a, vec2 b)
{
    Result res;
    //All this is basically Inigo's regular line SDF function - but store it in 'dist' instead: 
    vec2 ba = b-a;
    vec2 pa = p-a;
    float h = clamp(dot(pa, ba)/dot(ba, ba), 0.0, 1.0);
    res.dist = length(pa-h*ba);
    //Is the movement (a->b->p) a righthand turn? (cross product)
    res.side = sign((b.x - a.x)*(p.y - a.y) - (b.y - a.y)*(p.x - a.x));
    return res;
}

float sdfConvex4(vec2 point, vec2[4] points){

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
float roundedBoxSDF(vec2 center, vec2 size, vec4 radius)
{
    radius.xy = (center.x > 0.0) ? radius.xy : radius.zw;
    radius.x  = (center.y > 0.0) ? radius.x  : radius.y;

    vec2 q = abs(center)-size+radius.x;
    return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - radius.x;
}

// https://www.shadertoy.com/view/WtdSDs
vec4 applyBorderRadius(vec2 fragPosition, vec4 color, vec4 radius, vec2 size, mat3 transform){

    vec2 transformedFrag = applyTransform3(fragPosition, inverse(transform));

    // How soft the edges should be (in pixels). Higher values could be used to simulate a drop shadow.
    float edgeSoftness = 2.0f;

    vec2 halfSize = size / 2.0f;
    // Calculate distance to edge.   
    float distance = roundedBoxSDF(transformedFrag - halfSize, halfSize, radius);

    // Smooth the result (free antialiasing).
    float smoothedAlpha =  smoothstep(0.0f, edgeSoftness, distance);


    return mix(color, vec4(color.xyz, 0.0), smoothedAlpha);
}
