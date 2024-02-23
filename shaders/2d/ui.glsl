// layout(location = 0) in vec3 iLocation;
// layout(location = 1) in vec3 iColor;
// layout(location = 2) in vec3 iNormal;
// layout(location = 3) in vec2 iUV;
layout(set = 0, binding = 0) uniform  UiGlobalBuffer{   
	vec4 viewport;
	vec4 time;
} ui;

float mapRangeUnClamped(float value, float fromMin, float fromMax, float toMin, float toMax) {

    // Calculate the normalized position of the value in the input range
    float normalizedPosition = (value - fromMin) / (fromMax - fromMin);

    // Map the normalized position to the output range [toMin, toMax]
    return mix(toMin, toMax, normalizedPosition);
}

vec2 normalizePoint(vec4 viewport,vec2 point){
    return vec2(mapRangeUnClamped(point.x,0.0,viewport.z,-1.0,1.0),mapRangeUnClamped(point.y,0.0,viewport.w,-1.0,1.0));
}

bool shouldDiscard(vec4 viewport,vec4 clip,vec2 pixel){

    vec4 clip_ss = vec4(normalizePoint(viewport,clip.xy),normalizePoint(viewport,clip.xy + clip.zw));
    vec2 pixel_ss = normalizePoint(viewport,pixel);
    return pixel_ss.x > clip_ss.z || pixel_ss.x < clip_ss.x || pixel_ss.y < clip_ss.y || pixel_ss.y > clip_ss.w;
}
