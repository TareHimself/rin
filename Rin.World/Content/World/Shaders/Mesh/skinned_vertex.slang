#pragma once
#include "World/Shaders/Mesh/vertex.slang"

struct SkinnedVertex : IVertex {
    Vertex vertex;
    int4 boneIndices;
    float4 boneWeights;
    float3 getLocation() {
        return vertex.getLocation();
    }
    float3 getNormal() {
        return vertex.getNormal();
    }
    float2 getUV() {
        return vertex.getUV();
    }
    float3 getTangent() {
        return vertex.getTangent();
    }
};