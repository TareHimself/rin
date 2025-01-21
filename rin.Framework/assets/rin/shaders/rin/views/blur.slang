import rin.utils;

struct PushConstants
{
    float4x4 projection;
    float3x3 transform;
    float2 size;
    float sigma;
    float4 tint;
};

uniform Sampler2D SourceT;

[[vk::push_constant]]
uniform ConstantBuffer<PushConstants, ScalarDataLayout> push;

[shader("vertex")]
float4 vertex(int vertexId: SV_VertexID, out float2 oUV: UV)
    : SV_Position
{
    float4 position;
    generateRectVertex(push.size, push.projection, push.transform, vertexId, position, oUV);
    return position;
}

float normpdf(in float x, in float sigma)
{
    return 0.39894 * exp(-0.5 * x * x / (sigma * sigma)) / sigma;
}

[shader("fragment")]
float4 fragment(in float2 iUV: UV, float2 coordinate: SV_Position)
    : SV_Target
{
    
    uint width, height, levels;

    SourceT.GetDimensions(0, width, height, levels);

    var imageSize = float2(width,height);

    const int mSize = 11;
    const int kSize = (mSize - 1) / 2;
    float kernel[mSize];
    float3 final_colour = float3(0.0);

    // create the 1-D kernel
    float sigma = push.sigma;
    float Z = 0.0;
    for (int j = 0; j <= kSize; ++j)
    {
        kernel[kSize + j] = kernel[kSize - j] = normpdf(float(j), sigma);
    }

    // get the normalization factor (as the gaussian has been clamped)
    for (int j = 0; j < mSize; ++j)
    {
        Z += kernel[j];
    }

    // read out the texels
    for (int i = -kSize; i <= kSize; ++i)
    {
        for (int j = -kSize; j <= kSize; ++j)
        {
            final_colour += kernel[kSize + j] * kernel[kSize + i] * SourceT.Sample((coordinate.xy + float2(float(i), float(j))) / imageSize).rgb;
        }
    }

    return float4(final_colour / (Z * Z), 1.0) * push.tint;
}
