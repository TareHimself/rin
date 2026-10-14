using System.Linq;
using Rin.Shade.Transpiler;

namespace Rin.Shade.Tests.Emitter;

public class DoubleDiagnosticTests
{
    private const string Prelude = """
        using Rin.Shade;

        namespace DoubleCheck;

        public struct DoublePush
        {
            public float A;
            public BufferRef<float> Output;
        }

        """;

    [Test]
    public void A_double_literal_local_is_rejected()
    {
        var result = Emit("""
            [Shader]
            public class DoubleLocalShader : Shader
            {
                [Push] protected DoublePush Push;

                [Compute(1, 1, 1)]
                public void Compute()
                {
                    var half = 0.5;
                    Push.Output[0] = (float)half * Push.A;
                }
            }
            """);

        Assert.That(result.Diagnostics.Select(d => d.Id), Has.Member("SHADE0021"));
        Assert.That(result.Diagnostics.First(d => d.Id == "SHADE0021").GetMessage(), Does.Contain("0.5"));
    }

    [Test]
    public void A_double_parameter_is_rejected()
    {
        var result = Emit("""
            [Shader]
            public class DoubleParamShader : Shader
            {
                [Push] protected DoublePush Push;

                private static float Scale(double factor, float x)
                {
                    return (float)factor * x;
                }

                [Compute(1, 1, 1)]
                public void Compute()
                {
                    Push.Output[0] = Scale(2.0, Push.A);
                }
            }
            """);

        Assert.That(result.Diagnostics.Select(d => d.Id), Has.Member("SHADE0021"));
    }

    [Test]
    public void A_double_field_is_rejected()
    {
        var result = Emit("""
            public struct WidePush
            {
                public double A;
                public BufferRef<float> Output;
            }

            [Shader]
            public class DoubleFieldShader : Shader
            {
                [Push] protected WidePush Push;

                [Compute(1, 1, 1)]
                public void Compute()
                {
                    Push.Output[0] = 1f;
                }
            }
            """);

        Assert.That(result.Diagnostics.Select(d => d.Id), Has.Member("SHADE0021"));
    }

    [Test]
    public void Float_code_raises_nothing()
    {
        var result = Emit("""
            [Shader]
            public class FloatOnlyShader : Shader
            {
                [Push] protected DoublePush Push;

                [Compute(1, 1, 1)]
                public void Compute()
                {
                    var half = 0.5f;
                    Push.Output[0] = half * Push.A;
                }
            }
            """);

        Assert.That(result.Diagnostics, Is.Empty);
    }

    private static ShadeEmitResult Emit(string shader)
    {
        return ShadeEmitter.Emit(CompilationBuilder.Build(Prelude + shader));
    }
}
