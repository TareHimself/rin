using Rin.Shade.Transpiler;

namespace Rin.Shade.Tests.Emitter;

/// <summary>
/// C#'s operation tree has no parentheses, so the emitter has to put back exactly the ones the meaning
/// needs - and only those, so ordinary expressions stay readable.
/// </summary>
public class OperatorPrecedenceTests
{
    private static string Emit(string body)
    {
        var source = $$"""
            using System.Numerics;
            using Rin.Shade;

            namespace PrecedenceCheck;

            public struct PrecedencePush
            {
                public float A;
                public float B;
                public float C;
                public float D;
                public Vector4 V;
                public Vector4 W;
                public BufferRef<float> Output;
            }

            [Shader("Fixtures/precedence.slang")]
            public class PrecedenceShader : Shader
            {
                [Push] protected PrecedencePush Push;

                [Compute(1, 1, 1)]
                public void Compute()
                {
                    {{body}}
                }
            }
            """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));
        Assert.That(result.Diagnostics, Is.Empty);
        return result.Shaders["PrecedenceShader"];
    }

    [TestCase("Push.Output[0] = -(Push.A + Push.B);", "= -(push.a + push.b);")]
    [TestCase("Push.Output[0] = Push.A - (Push.B - Push.C);", "= push.a - (push.b - push.c);")]
    [TestCase("Push.Output[0] = (Push.A + Push.B) * Push.C;", "= (push.a + push.b) * push.c;")]
    [TestCase("Push.Output[0] = 1f / (Push.A * Push.B + Push.C);", "= 1 / (push.a * push.b + push.c);")]
    [TestCase("Push.Output[0] = Push.A / (Push.B * Push.C);", "= push.a / (push.b * push.c);")]
    [TestCase("Push.Output[0] = (Push.A > Push.B ? Push.A : Push.B) + Push.C;",
        "= (push.a > push.b ? push.a : push.b) + push.c;")]
    [TestCase("Push.Output[0] = (Push.V + Push.W).X;", "= (push.v + push.w).x;")]
    [TestCase("Push.Output[0] = (float)(int)(Push.A + Push.B);", "= (float)(int)(push.a + push.b);")]
    public void KeepsTheParenthesesTheMeaningNeeds(string body, string expected)
    {
        Assert.That(Emit(body), Does.Contain(expected));
    }

    [TestCase("Push.Output[0] = Push.A * Push.B + Push.C * Push.D;", "= push.a * push.b + push.c * push.d;")]
    [TestCase("Push.Output[0] = Push.A + Push.B + Push.C;", "= push.a + push.b + push.c;")]
    [TestCase("Push.Output[0] = -Push.A + Push.B;", "= -push.a + push.b;")]
    [TestCase("Push.Output[0] = Push.A - Push.B * Push.C;", "= push.a - push.b * push.c;")]
    public void LeavesUnnecessaryParenthesesOut(string body, string expected)
    {
        Assert.That(Emit(body), Does.Contain(expected));
    }
}
