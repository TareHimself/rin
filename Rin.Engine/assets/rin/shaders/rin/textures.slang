import rin.attributes;
#ifndef RIN_GLOBAL_TEXTURES_COUNT
#define RIN_GLOBAL_TEXTURES_COUNT 2048
#endif
// Define a texture array resource
//[layout(set = 0, binding = 0), $partial, $update, $variable = 512, $stage = all]

[[vk::binding(0, 0)]]
[AllStages, UpdateAfterBind, Partial, Variable(RIN_GLOBAL_TEXTURES_COUNT)]
uniform Sampler2D GLOBAL_TEXTURES[];




// Function to sample a texture
float4 sampleTexture(int textureId, float2 uv)
{
    return GLOBAL_TEXTURES[textureId].Sample(uv);
}

// Function to get the size of a texture
uint2 getTextureSize(int textureId, uint mip = 0)
{
    uint2 size;
    uint levels;
    GLOBAL_TEXTURES[textureId].GetDimensions(mip, size.x, size.y, levels);
    return size;
}
