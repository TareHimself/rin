
#include "Framework/Shaders/Attributes/blending.slang"
#include "Framework/Shaders/images.slang"
#include "Framework/Shaders/utils.slang"

struct Rect
{
    float2 offset;
    float2 size;
}
struct BlurData
{
    float4x4 transform;
    float4x4 projection;
    ImageHandle SourceImageHandle;
    float2 size;
    float strength;
    float2 radius;
    float4 tint;
    float4 destRect;
};

struct PushConstants
{
    BlurData *data;
    int isHorizontal;
};

[[vk::push_constant]]
uniform ConstantBuffer<PushConstants, ScalarDataLayout> push;

[shader("vertex")]
float4 vertex(int vertexId: SV_VertexID, out float2 oUV: UV)
    : SV_Position
{
    float4 position;
    generateRectVertex(push.data->size, push.data->projection, push.data->transform, vertexId, position, oUV);
    return position;
}

float normpdf(in float x, in float sigma)
{
    return 0.39894 * exp(-0.5 * x * x / (sigma * sigma)) / sigma;
}

// Gaussian function
float gaussianWeight(float x, float sigma)
{
    return exp(-(x * x) / (2.0 * sigma * sigma)) / (sqrt(2.0 * 3.14159265359) * sigma);
}

float distanceFromZero(float x, float y)
{
    return sqrt((x * x) + (y * y));
}

// float2 getUv(float2 uv,float4 uvRect){
//     var result = mapRangeUnClamped(uv,uvRect.xy,uvRect.zw)
// }
[shader("fragment"), Attachment(AttachmentFormat.RGBA32), BlendUI, Stencil]
float4 fragment(in float2 iUV: UV, float2 coordinate: SV_Position)
    : SV_Target
{

    var data = push.data[0];

    var handle = push.data->SourceImageHandle;

    var size = getTextureSize(push.data->SourceImageHandle);

    var imageSize = float2(size.x, size.y);

    var p1 = data.destRect.xy;
    var p2 = data.destRect.zw;

    var imageUV = mapRangeUnClamped(coordinate,p1,p2, float2(0.0f), float2(1.0f));
    // imageUV = min(imageUV,1 - imageUV);
    // return float4(float3(max(imageUV.x,imageUV.y)), 1.0);

    float2 uvRadius = 1.0 / imageSize;

    var thisColor = handle.SampleTexture(imageUV, ImageTiling.ClampEdge);

    float3 color = float3(0.0);

    float weightSum = 0;
    
    switch (push.isHorizontal)
    {
    case 0: // Vertical
    {
        float radius = push.data->radius.y;

        int kernelRadius = int(radius);

        float sigma = radius * 0.5 * push.data->strength;

        var x = 0;
        for (int y = -kernelRadius; y <= kernelRadius; y++)
        {
            var dist = distanceFromZero(x, y);
            var uv = float2(x * uvRadius.x, y * uvRadius.y);
            float weight = normpdf(dist, sigma);
            color += handle.SampleTexture(imageUV + uv, ImageTiling.ClampEdge).rgb * weight;
            weightSum += weight;
        }
    }
    break;
    case 1: // Horizontal
    {
        float radius = push.data->radius.x;

        int kernelRadius = int(radius);

        float sigma = radius * 0.5 * push.data->strength;

        var y = 0;

        for (int x = -kernelRadius; x <= kernelRadius; x++)
        {
            var dist = distanceFromZero(x, y);
            var uv = float2(x * uvRadius.x, y * uvRadius.y);
            float weight = normpdf(dist, sigma);
            color += handle.SampleTexture(imageUV + uv, ImageTiling.ClampEdge).rgb * weight;
            weightSum += weight;
        }
    }
    break;
    }

    return float4(color / max(weightSum, 1e-5), thisColor.a);
}
