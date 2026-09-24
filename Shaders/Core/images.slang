#pragma once
#include "Shaders/Core/defs.slang"
#include "Shaders/Core/attributes.slang"

// Mirrors Rin.Core.Graphics.ResourceType bit-for-bit - order matters, this is decoded from raw bits.
enum ResourceType {
    Texture,
    Cubemap,
    TextureArray,
    Buffer
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

// Mirrors Rin.Core.Graphics.DeviceHandle bit-for-bit.
struct DeviceHandle {
    int _data;

    __init(int data) {
        _data = data;
    }

    __init(ResourceType type, int id) {
        _data = ((id & 0xFFFFFF) << 8) | ((int)type & 0xFF);
    }

    int GetId() {
        return (_data >> 8) & 0xFFFFFF;
    }

    ResourceType GetType() {
        return (ResourceType)(_data & 0x7F);
    }

    bool IsValid() {
        return GetId() > 0;
    }

    float4 SampleTexture(float2 uv, ImageTiling tiling = ImageTiling.Repeat, ImageFilter filter = ImageFilter.Linear) {
        return sampleTexture(this, uv, tiling, filter);
    }

    // Fetch the exact texel at integer pixel coordinates — no filtering, no sampler.
    // Used by algorithms (e.g. SLUG) that treat textures as data arrays rather than images.
    float4 TexelLoad(int2 loc) {
        return READ_TEXTURES[NonUniformResourceIndex(GetId())].Load(int3(loc, 0));
    }
}

[[_RIN_INTERNAL_BINDING(0, 0)]]
[SamplerBinding,AllStages, UpdateAfterBind, Partial]
uniform SamplerState SAMPLERS[RIN_GLOBAL_SAMPLER_COUNT];

[[_RIN_INTERNAL_BINDING(0, 1)]]
[TextureBinding,AllStages, UpdateAfterBind, Partial]
uniform Texture2D READ_TEXTURES[RIN_GLOBAL_TEXTURE_COUNT];

[[_RIN_INTERNAL_BINDING(0, 2)]]
[TextureArrayBinding,AllStages, UpdateAfterBind, Partial]
uniform Texture2DArray READ_TEXTURE_ARRAYS[RIN_GLOBAL_TEXTURE_ARRAY_COUNT];

[[_RIN_INTERNAL_BINDING(0, 3)]]
[CubemapBinding,AllStages, UpdateAfterBind, Partial]
uniform TextureCube READ_CUBEMAPS[RIN_GLOBAL_CUBEMAP_COUNT];

SamplerState getSampler(ImageTiling tiling = ImageTiling.Repeat,ImageFilter filter = ImageFilter.Linear) {
    return SAMPLERS[((int)filter * 2) + (int)tiling];
}

float4 sampleTexture(DeviceHandle handle, float2 uv, ImageTiling tiling = ImageTiling.Repeat, ImageFilter filter = ImageFilter.Linear)
{
    var sampler = getSampler(tiling, filter);
    return READ_TEXTURES[NonUniformResourceIndex(handle.GetId())].Sample(sampler,uv);
}

// Function to get the size of a texture
uint2 getTextureSize(DeviceHandle handle, uint mip = 0)
{
    uint2 size;
    uint levels;
    READ_TEXTURES[NonUniformResourceIndex(handle.GetId())].GetDimensions(mip, size.x, size.y, levels);
    return size;
}

