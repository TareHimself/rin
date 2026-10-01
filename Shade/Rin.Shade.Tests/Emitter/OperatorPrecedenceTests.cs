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
                public Matrix4x4 M1;
                public Matrix4x4 M2;
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
    [TestCase("Push.Output[0] = 1f / (Push.A * Push.B + Push.C);", "= 1.0 / (push.a * push.b + push.c);")]
    [TestCase("Push.Output[0] = 1f / 3f;", "= 1.0 / 3.0;")]
    [TestCase("Push.Output[0] = 2f / 65536f * Push.A;", "= 2.0 / 65536.0 * push.a;")]
    [TestCase("Push.Output[0] = Push.A / (Push.B * Push.C);", "= push.a / (push.b * push.c);")]
    [TestCase("Push.Output[0] = (Push.A > Push.B ? Push.A : Push.B) + Push.C;",
        "= (push.a > push.b ? push.a : push.b) + push.c;")]
    [TestCase("Push.Output[0] = (Push.V + Push.W).X;", "= (push.v + push.w).x;")]
    [TestCase("Push.Output[0] = (float)(int)(Push.A + Push.B);", "= (float)(int)(push.a + push.b);")]
    public void KeepsTheParenthesesTheMeaningNeeds(string body, string expected)
    {
        Assert.That(Emit(body), Does.Contain(expected));
    }

    [TestCase("Push.Output[0] = (Push.M1 + Push.M2).Row(0).X;", "= (push.m1 + push.m2)[0].x;")]
    [TestCase("Push.Output[0] = Push.M1.Row(1).X;", "= push.m1[1].x;")]
    [TestCase("Push.Output[0] = Math.Abs(Push.A + Push.B);", "= abs(push.a + push.b);")]
    [TestCase("Push.Output[0] = Math.Clamp(Push.A + 1f, Push.B, Push.C * Push.D);",
        "= clamp(push.a + 1, push.b, push.c * push.d);")]
    public void TemplateArgumentsAreGroupedOnlyWhereTheTemplateNeedsIt(string body, string expected)
    {
        Assert.That(Emit(body), Does.Contain(expected));
    }

    [Test]
    public void NamedTemplatePlaceholdersResolveToParametersAndUnknownOnesAreErrors()
    {
        var source = """
            using System.Numerics;
            using Rin.Shade;

            namespace NamedCheck;

            public static class Named
            {
                [SlangExpression("@matrix[@index]")]
                public static Vector4 At(Matrix4x4 matrix, int index) => default;

                [SlangExpression($"@{nameof(matrix)}.x[@{nameof(index)}]")]
                public static float Renamed(Matrix4x4 matrix, int index) => default;

                [SlangExpression("@matrix[@idx]")]
                public static Vector4 Bad(Matrix4x4 matrix, int index) => default;
            }

            public struct NamedPush { public Matrix4x4 M; public BufferRef<float> Output; }

            [Shader("Fixtures/named.slang")]
            public class NamedShader : Shader
            {
                [Push] protected NamedPush Push;

                [Compute(1, 1, 1)]
                public void Compute()
                {
                    Push.Output[0] = Named.At(Push.M, 1).X;
                    Push.Output[1] = Named.Bad(Push.M, 1).X;
                    Push.Output[2] = Named.Renamed(Push.M, 2);
                }
            }
            """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));
        Assert.That(result.Shaders["NamedShader"], Does.Contain("push.m[1].x"));
        Assert.That(result.Shaders["NamedShader"], Does.Contain("push.m.x[2]"));
        Assert.That(result.Diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "SHADE0020" }));
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
