#pragma once

interface IVertex {
    float3 getLocation();
    float2 getUV();
    float3 getTangent();
}

struct Vertex : IVertex {
    float4 _locationU;
    float4 _normalV;
    float4 _tangent;
    float3 getLocation() {
        return _locationU.xyz;
    }
    float3 getNormal() {
        return _normalV.xyz;
    }
    float2 getUV() {
        return float2(_locationU.w,_normalV.w);
    }
    float3 getTangent() {
        return _tangent.xyz;
    }
};