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

enum ImageFilter {
    Linear,
    Nearest
}

enum ImageTiling {
    Repeat,
    ClampEdge,
    ClampBorder
}



[[vk::binding(0, 0)]]
[SamplerBinding,AllStages, UpdateAfterBind, Partial]
uniform SamplerState SAMPLERS[6];

[[vk::binding(1, 0)]]
[TextureBinding,AllStages, UpdateAfterBind, Partial, Variable(RIN_GLOBAL_TEXTURES_COUNT)]
uniform Texture2D READ_TEXTURES[];

SamplerState getSampler(ImageTiling tiling = ImageTiling.Repeat,ImageFilter filter = ImageFilter.Linear) {
    return SAMPLERS[((int)filter * 2) + (int)tiling];
}

float4 sampleTexture(ImageHandle handle, float2 uv, ImageTiling tiling = ImageTiling.Repeat, ImageFilter filter = ImageFilter.Linear)
{
    var sampler = getSampler(tiling, filter);
    return READ_TEXTURES[handle.GetId()].Sample(sampler,uv);
}

// Function to get the size of a texture
uint2 getTextureSize(ImageHandle handle, uint mip = 0)
{
    uint2 size;
    uint levels;
    READ_TEXTURES[handle.GetId()].GetDimensions(mip, size.x, size.y, levels);
    return size;
}

struct ImageHandle {
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

    float4 SampleTexture(float2 uv, ImageTiling tiling = ImageTiling.Repeat, ImageFilter filter = ImageFilter.Linear) {
        return sampleTexture(this, uv,tiling, filter);
    }
}