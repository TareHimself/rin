namespace Rin::Shade::Tests::Fixtures
{
    struct GraphicsPushConstants
    {
        float4 color;
    }

    struct VsIn
    {
        int vertexId : SV_VertexID;
    }

    struct VsOut
    {
        float2 uv : UV;
        float4 position : SV_Position;
    }

    struct FsIn
    {
        float2 uv : UV;
    }

}

using namespace Rin::Shade::Tests::Fixtures;

[[vk::push_constant]] uniform ConstantBuffer<GraphicsPushConstants, ScalarDataLayout> push;

[shader("vertex")]
VsOut vertex(VsIn input)
{
    VsOut output;
    output.uv = float2(0, 0);
    output.position = float4(0, 0, 0, 1);
    return output;
}

[shader("fragment")]
float4 fragment(FsIn input)
{
    return push.color;
}
