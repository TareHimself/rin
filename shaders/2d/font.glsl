
#define MAX_NUM_CHARACTERS = 256;
#define MAX_NUM_ATLAS = 3;

float mapRangeUnClamped(float value, float fromMin, float fromMax, float toMin, float toMax) {

    // Calculate the normalized position of the value in the input range
    float normalizedPosition = (value - fromMin) / (fromMax - fromMin);

    // Map the normalized position to the output range [toMin, toMax]
    return mix(toMin, toMax, normalizedPosition);
}

struct FontChar {
	vec4 uv;
    vec4 extras;
};

layout(set = 1, binding = 0) uniform sampler2D AtlasT;
layout(set = 1, binding = 1) uniform  FontChars{   
	FontChar chars[256];
    int numChars;
} font;


layout( push_constant ) uniform constants
{
    vec4 extent;
	int idx;
} pFont;