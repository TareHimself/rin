
#define MAX_NUM_CHARACTERS = 256;
#define MAX_NUM_ATLAS = 3;

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