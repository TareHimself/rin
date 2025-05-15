#include "Engine/Shaders/utils.slang"
#include "Engine/Shaders/textures.slang"

struct BlurData
{
    float4x4 projection;
    float4x4 transform;
    ImageHandle SourceImageHandle;
    float4 _options;
    float4 tint;
    float2 getSize() {
        return _options.xy;
    }
    float getStrength() {
        return _options.y;
    }
    float getRadius() {
        return _options.w;
    }
};

struct PushConstants
{
    BlurData *data;
};

[[vk::push_constant]]
uniform ConstantBuffer<PushConstants, ScalarDataLayout> push;

[shader("vertex")]
float4 vertex(int vertexId: SV_VertexID, out float2 oUV: UV)
    : SV_Position
{
    float4 position;
    generateRectVertex(push.data->getStrength(), push.data->projection, push.data->transform, vertexId, position, oUV);
    return position;
}

float normpdf(in float x, in float sigma)
{
    return 0.39894 * exp(-0.5 * x * x / (sigma * sigma)) / sigma;
}

// Gaussian function
float gaussianWeight(float x, float sigma) {
    return exp(-(x * x) / (2.0 * sigma * sigma)) / (sqrt(2.0 * 3.14159265359) * sigma);
}

float distanceFromZero(float x, float y) {
    return sqrt((x * x) + (y * y));
}

[shader("fragment")]
float4 fragment(in float2 iUV: UV, float2 coordinate: SV_Position)
    : SV_Target
{

    uint width, height, levels;
    var handle = push->data.SourceImageHandle;

    var size = getTextureSize(push.data->SourceImageHandle);
    
    var imageSize = float2(width, height);

    var imageUV = coordinate / imageSize;

    // return float4(imageUV,0.0f, 1.0);
    float2 uvRadius = 1.0 / imageSize;
    float4 color = 0;
    float weightSum = 0;

    float sigma = push.data->getRadius() * 0.5 * push.data->getStrength();
    int kernelRadius = int(push.data->getRadius());

    // for (int i = -kernelRadius; i <= kernelRadius; i++) {
    //     float weight = gaussianWeight(i, sigma);
    //     color += SourceT.Sample(imageUV + float2(i * uvRadius.x, 0)).rgb * weight;
    //     color += SourceT.Sample(imageUV + float2(0, i * uvRadius.y)).rgb * weight;
    //     weightSum += 2.0 * weight;
    // }

    for (int x = -kernelRadius; x <= kernelRadius; x++) {
        for (int y = -kernelRadius; y <= kernelRadius; y++) {
            var dist = distanceFromZero(x, y);
            var uv = float2(x * uvRadius.x, y * uvRadius.y);
            float weight = normpdf(dist, sigma);
            color += handle.SampleTexture(imageUV + uv,ImageTiling.ClampEdge) * weight;
            weightSum += weight;
        }
    }

    return color / weightSum;
}