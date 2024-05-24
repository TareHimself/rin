#define MAX_NUM_CHARACTERS = 256;
#define MAX_NUM_ATLAS = 3;
#extension GL_EXT_scalar_block_layout : require

layout(set = 1, binding = 0) uniform sampler2D TAtlas[256];


layout(push_constant, scalar) uniform constants
{
    mat3 transform;
    vec2 size;
    int textureIdx;
} pFont;