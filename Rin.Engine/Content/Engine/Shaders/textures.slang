#include "Engine/Shaders/attributes.slang"

#ifndef RIN_GLOBAL_TEXTURES_COUNT
#define RIN_GLOBAL_TEXTURES_COUNT 2048
#endif
// Define a texture array resource
//[layout(set = 0, binding = 0), $partial, $update, $variable = 512, $stage = all]

[[vk::binding(0, 0)]]
[AllStages, UpdateAfterBind, Partial, Variable(RIN_GLOBAL_TEXTURES_COUNT)]
uniform Sampler2D TEXTURES[];

// [[vk::binding(0,1)]]
// [AllStages, UpdateAfterBind, Partial, Variable(RIN_GLOBAL_TEXTURES_COUNT)]
// uniform Sampler3D CUBEMAPS[];

// Function to sample a texture
float4 sampleTexture(int textureId, float2 uv)
{
    return TEXTURES[textureId].Sample(uv);
}

// Function to get the size of a texture
uint2 getTextureSize(int textureId, uint mip = 0)
{
    uint2 size;
    uint levels;
    TEXTURES[textureId].GetDimensions(mip, size.x, size.y, levels);
    return size;
}
