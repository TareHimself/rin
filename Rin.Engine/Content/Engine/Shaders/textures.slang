#pragma once
#include "Engine/Shaders/attributes.slang"

#ifndef RIN_GLOBAL_TEXTURES_COUNT
#define RIN_GLOBAL_TEXTURES_COUNT 2048
#endif

enum ImageType {
    Image,
    Cube,
    Volume
}

struct TextureHandle {
    int _data;
    int GetId() {
        return (_data >> 8) & 0xFFFFFF;
    }

    ImageType GetType() {
        return (ImageType)(_data & 0xFF);
    }

    bool IsValid() {
        return GetId() > 0;
    }
}
// Define a texture array resource
//[layout(set = 0, binding = 0), $partial, $update, $variable = 512, $stage = all]

[[vk::binding(0, 0)]]
[AllStages, UpdateAfterBind, Partial, Variable(RIN_GLOBAL_TEXTURES_COUNT)]
uniform Sampler2D TEXTURES[];

// [[vk::binding(0,1)]]
// [AllStages, UpdateAfterBind, Partial, Variable(RIN_GLOBAL_TEXTURES_COUNT)]
// uniform Sampler3D CUBEMAPS[];

// Function to sample a texture
float4 sampleTexture(TextureHandle handle, float2 uv)
{
    return TEXTURES[handle.GetId()].Sample(uv);
}

// Function to get the size of a texture
uint2 getTextureSize(TextureHandle handle, uint mip = 0)
{
    uint2 size;
    uint levels;
    TEXTURES[handle.GetId()].GetDimensions(mip, size.x, size.y, levels);
    return size;
}
