#pragma once
#include "Framework/Shaders/images.slang"

struct GBufferSample {
    float3 color;
    float3 location;
    float3 normal;
    float roughness;
    float metallic;
    float specular;
    float emissive;
};

struct GBuffer {
    ImageHandle G_BUFFER_0;
    ImageHandle G_BUFFER_1;
    ImageHandle G_BUFFER_2;

    GBufferSample sample(in float2 uv, ImageTiling tiling = ImageTiling.ClampEdge, ImageFilter filter = ImageFilter.Linear) {
        var sample0 = G_BUFFER_0.SampleTexture(uv, tiling, filter);
        var sample1 = G_BUFFER_1.SampleTexture(uv, tiling, filter);
        var sample2 = G_BUFFER_2.SampleTexture(uv, tiling, filter);
        // sample0.rgb
        return GBufferSample(sample0.rgb, sample1.rgb, sample2.rgb, sample0.a, sample1.a, sample2.a, 0.0);
    }
};



