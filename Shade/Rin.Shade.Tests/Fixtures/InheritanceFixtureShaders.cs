using Rin.Shade;
using Rin.Shade.Tests;

namespace Rin.Shade.Tests.Fixtures;

public struct InheritancePushConstants
{
    public float Value;
    public TestBuffer<float> Output;
}

public abstract class BaseInheritanceShader : Shader
{
    [Push] protected InheritancePushConstants Push;

    [Compute(1, 1, 1)]
    public virtual void Compute()
    {
        Push.Output[0] = Push.Value;
    }
}

[Shader]
public class DerivedInheritanceShader : BaseInheritanceShader
{
    public override void Compute()
    {
        Push.Output[0] = Push.Value + 1f;
    }
}
