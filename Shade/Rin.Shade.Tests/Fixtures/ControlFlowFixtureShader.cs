using System.Runtime.CompilerServices;
using Rin.Shade;

namespace Rin.Shade.Tests.Fixtures;

[InlineArray(4)]
public struct FourFloats
{
    private float _element;
}

public struct Weights
{
    public FourFloats Values;
}

public struct ControlFlowPushConstants
{
    public Weights Weights;
    public uint Count;
    public BufferRef<float> Output;
}

[Shader("Fixtures/control_flow.slang")]
public class ControlFlowFixtureShader : Shader
{
    [Push] protected ControlFlowPushConstants Push;

    [Compute(1, 1, 1)]
    public void Compute()
    {
        var sum = 0f;

        for (var i = 0; i < 4; i++)
        {
            if (i == 2) continue;
            sum = sum + i;
        }

        var j = 0;
        while (j < (int)Push.Count)
        {
            if (j > 10) break;
            j = j + 1;
        }

        switch ((int)Push.Count)
        {
            case 0:
                sum = sum + 1f;
                break;
            default:
                sum = sum + 2f;
                break;
        }

        foreach (var weight in Push.Weights.Values)
        {
            sum = sum + weight;
        }

        float Double(float x) => x * 2f;
        sum = Double(sum);

        Push.Output[0] = sum;
    }
}
