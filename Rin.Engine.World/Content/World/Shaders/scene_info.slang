#include "World/Shaders/Lights/light.slang"
#pragma once
struct SceneInfo
{
    float4x4 view;
    float4x4 projection;
    float4x4 viewProjection;
    float3 cameraPosition;
    PackedLight *lights;
    int numLights;
}