#version 450

#extension GL_GOOGLE_include_directive : require

float mapRangeUnClamped(float value, float fromMin, float fromMax, float toMin, float toMax) {

    // Calculate the normalized position of the value in the input range
    float normalizedPosition = (value - fromMin) / (fromMax - fromMin);

    // Map the normalized position to the output range [toMin, toMax]
    return mix(toMin, toMax, normalizedPosition);
}

layout(set = 2, binding = 0) uniform sampler2D ImageT;
layout (location = 0) in vec2 iUV;
layout (location = 0) out vec4 oColor;

//push constants block
layout( push_constant ) uniform constants
{
	vec4 extent;
    vec4 time;
} pRect;

void main() {
	vec2 uv = vec2(mapRangeUnClamped(iUV.x,0.0,1.0,0.5,1.0),mapRangeUnClamped(iUV.y,0.0,1.0,0.5,1.0));
	oColor = texture(ImageT,uv);
}