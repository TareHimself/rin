using Rin.Shade.Transpiler;

namespace Rin.Shade.Tests.Emitter;

public class OutVarAndNamedArgumentTests
{
    private const string Source = """
        using Rin.Shade;

        namespace OutVarCheck;

        public struct OutPush
        {
            public float A;
            public BufferRef<float> Output;
        }

        [Shader("Fixtures/out_var.slang")]
        public class OutVarShader : Shader
        {
            [Push] protected OutPush Push;

            private static void Split(float value, out float whole, out float fraction)
            {
                whole = Math.Floor(value);
                fraction = value - whole;
            }

            private static float Weighted(float first, float second, float weight)
            {
                return first * (1f - weight) + second * weight;
            }

            [Compute(1, 1, 1)]
            public void Compute()
            {
                Split(Push.A, out var whole, out var fraction);
                Push.Output[0] = whole + fraction;

                if (Weighted(first: 1f, weight: 0.25f, second: 3f) > 1f)
                {
                    Split(Push.A * 2f, out var inner, out var rest);
                    Push.Output[1] = inner + rest;
                }
            }
        }
        """;

    [Test]
    public void OutVarDeclaresTheLocalBeforeTheStatementAndPassesItsName()
    {
        var result = ShadeEmitter.Emit(CompilationBuilder.Build(Source));

        Assert.That(result.Diagnostics, Is.Empty);
        var slang = result.Shaders["OutVarShader"].Replace("\r\n", "\n");
        Assert.That(slang, Does.Contain("    float whole;\n    float fraction;\n    split(push.a, whole, fraction);"));
        Assert.That(slang, Does.Contain("        float inner;\n        float rest;\n        split(push.a * 2, inner, rest);"));
    }

    [Test]
    public void NamedArgumentsAreEmittedInParameterOrder()
    {
        var result = ShadeEmitter.Emit(CompilationBuilder.Build(Source));

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Shaders["OutVarShader"], Does.Contain("weighted(1, 3, 0.25)"));
    }
}
