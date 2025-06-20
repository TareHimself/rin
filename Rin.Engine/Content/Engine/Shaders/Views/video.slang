#include "Engine/Shaders/utils.slang"
#include "Engine/Shaders/sd.slang"
#include "Engine/Shaders/images.slang"
#include "Engine/Shaders/Attributes/blending.slang"

struct VideoItem {
    float4x4 transform;
    float2 size;
    ImageHandle frame;
}

struct PushConstants
{
    float4x4 projection;
    VideoItem *items;
};

[[vk::push_constant]]
uniform ConstantBuffer<PushConstants, ScalarDataLayout> push;

struct VSIn
{
    int instanceId : SV_InstanceID;
    int vertexId : SV_VertexID;
}

struct VSOut
{
    float2 uv : UV;
    int index : INDEX;
    float4 position : SV_Position;
}

struct FSIn
{
    float2 uv : UV;
    int index : INDEX;
    float2 coordinate : SV_Position;
}

[shader("vertex")]
VSOut vertex(VSIn input)

{
    VSOut output;
    output.index = input.instanceId;
    var item = push.items[input.instanceId];
    generateRectVertex(item.size, push.projection, item.transform, input.vertexId, output.position, output.uv);
    return output;
}


[shader("fragment"),Attachment(AttachmentFormat.RGBA32), BlendUI,Stencil]
float4 fragment(FSIn input)
    : SV_Target
{
    var item = push.items[input.index];
    var color = item.frame.SampleTexture(input.uv, ImageTiling.ClampEdge, ImageFilter.Linear).rgba;
    return color;
}
