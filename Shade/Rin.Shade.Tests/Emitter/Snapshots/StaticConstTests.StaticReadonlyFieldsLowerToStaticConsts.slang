namespace StaticConstCheck
{
    struct Kernel
    {
        float scale;

        static const int taps = 3;
        static const float2 step = float2(1, 0) * 2;
    }

    struct ConstPush
    {
        Kernel kernel;
        float3* output;
    }

}

using namespace StaticConstCheck::StaticConstShader;
using namespace StaticConstCheck;

namespace StaticConstCheck::Lighting
{
    static const float weights[3] = { 0.25, 0.5, 0.25 };
    static const float ambient = 0.12;
    static const float diffuse = 1 - ambient;
    static const float3 up = float3(0, 1, 0);
    static const float3 lightDirection = float3(-0.45, 0.6, 0.65);
}
namespace StaticConstCheck::StaticConstShader
{
    static const float3 tint = float3(0.9, 0.45, 0.2);
}

[[vk::push_constant]] uniform ConstantBuffer<ConstPush, ScalarDataLayout> push;

[shader("compute")]
[numthreads(1, 1, 1)]
void compute()
{
    float sum = 0;
    for (var i = 0; i < 3; i++)
    {
        sum += Lighting::weights[i];
    }
    var shade = Lighting::ambient + Lighting::diffuse * max(dot(Lighting::up, Lighting::lightDirection), 0);
    push.output[0] = tint * shade * sum * Kernel.taps * push.kernel.scale + float3(Kernel.step, 0);
}
