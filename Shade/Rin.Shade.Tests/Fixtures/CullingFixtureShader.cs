using System.Numerics;
using Rin.Shade;

namespace Rin.Shade.Tests.Fixtures;

[ShaderStruct]
public struct Bounds3D
{
    public Vector3 Min;
    public Vector3 Max;
}

public struct CullingPushConstants
{
    public BufferRef<Bounds3D> Bounds;
    public uint InvocationCount;
    public BufferRef<uint> Output;
}

public struct ComputeIn
{
    [Semantic("SV_DispatchThreadID")] public uint ThreadId;
}

[Shader("Fixtures/culling.slang")]
public class CullingFixtureShader : Shader
{
    [Push] protected CullingPushConstants Push;

    [Compute(64, 1, 1)]
    public void Compute(ComputeIn input)
    {
        if (input.ThreadId >= Push.InvocationCount) return;
        var bounds = Push.Bounds[(int)input.ThreadId];
        Push.Output[(int)input.ThreadId] = 1;
    }
}
