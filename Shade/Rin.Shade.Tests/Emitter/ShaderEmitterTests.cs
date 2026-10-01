using System.Linq;
using Rin.Shade.Transpiler;

namespace Rin.Shade.Tests.Emitter;

public class ShaderEmitterTests
{
    [Test]
    public void EmitsCullingFixtureShader()
    {
        var source = FixtureSource.Read("../Fixtures/CullingFixtureShader.cs");
        var compilation = CompilationBuilder.Build(source);

        var result = ShadeEmitter.Emit(compilation);

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Shaders.Keys, Is.EquivalentTo(new[] { "CullingFixtureShader" }));

        Snapshot.Verify(result.Shaders["CullingFixtureShader"]);
    }

    [Test]
    public void RejectedConstructProducesLocatedDiagnostic()
    {
        const string source = """
                               using Rin.Shade;

                               namespace RejectedConstruct;

                               [Shader("Fixtures/rejected.slang")]
                               public class RejectedConstructShader : Shader
                               {
                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       try
                                       {
                                       }
                                       catch
                                       {
                                       }
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics.Any(d => d.Id == Diagnostics.Emitter.UnsupportedConstruct.Id), Is.True);
    }

    [Test]
    public void SlangExpressionStubIsSubstitutedIntoTheCallSite()
    {
        const string source = """
                               using Rin.Shade;

                               namespace SlangExpressionMechanism;

                               public static class TestIntrinsics
                               {
                                   [SlangExpression("max(@0, @1)")]
                                   public static extern float Max(float a, float b);
                               }

                               public struct MaxPush
                               {
                                   public float A;
                                   public float B;
                               }

                               [Shader("Fixtures/max.slang")]
                               public class MaxShader : Shader
                               {
                                   [Push] protected MaxPush Push;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       var result = TestIntrinsics.Max(Push.A, Push.B);
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Shaders["MaxShader"], Does.Contain("var result = max(push.a, push.b);"));
    }
}
