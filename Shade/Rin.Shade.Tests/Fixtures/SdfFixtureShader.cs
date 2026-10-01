using System.Numerics;
using Rin.Shade;

namespace Rin.Shade.Tests.Fixtures;

public struct SdfPushConstants
{
    public Vector2 P;
    public Vector2 A;
    public Vector2 B;
    public BufferRef<float> Output;
}

[Shader("Fixtures/sdf.slang")]
public class SdfFixtureShader : Shader
{
    [Push] protected SdfPushConstants Push;

    [Compute(1, 1, 1)]
    public void Compute()
    {
        Push.Output[0] = Sd.SdSegment(Push.P, Push.A, Push.B);
    }
}
