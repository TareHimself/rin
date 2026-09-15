import rin.utils;
uniform Sampler2D SourceT;

struct MeshData {
    
};

struct PushConstants
{
    float4x4 projection;
    float3x3 transform;
};

struct VertexIn {
    int vertexId : SV_VertexID;
};

struct VertexOut {
    float2 uv : UV;
    float4 position : SV_Position;
};

struct FragmentIn {
    float2 uv : UV;
    float2 coordinate : SV_Position;
};

struct FragmentOut {
    float4 output : SV_Target;
};

[[vk::push_constant]]
uniform ConstantBuffer<PushConstants, ScalarDataLayout> push;

[shader("vertex")]
VertexOut vertex(VertexIn input) {
    VertexOut output;
    return output;
}

[shader("fragment")]
FragmentOut fragment(FragmentIn input)
{
    FragmentOut output;
    return output;
}