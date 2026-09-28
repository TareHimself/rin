using System.Linq;
using Rin.Shade.Transpiler;

namespace Rin.Shade.Tests.Emitter;

public class FunctionLoweringTests
{
    [Test]
    public void WalksMultiLevelCallsAndPrunesDeadCode()
    {
        var sources = new[]
        {
            FixtureSource.Read("../Fixtures/SdFixtures.cs"),
            FixtureSource.Read("../Fixtures/SdfFixtureShader.cs")
        };
        var compilation = CompilationBuilder.Build(sources);

        var result = ShadeEmitter.Emit(compilation);

        Assert.That(result.Diagnostics, Is.Empty);
        var slang = result.Shaders["SdfFixtureShader"];

        Assert.That(slang, Does.Contain("float dot2(float2 v)"));
        Assert.That(slang, Does.Contain("float sdSegment(float2 p, float2 a, float2 b)"));
        Assert.That(slang.IndexOf("float dot2("), Is.LessThan(slang.IndexOf("float sdSegment(")),
            "dot2 is a dependency of sdSegment and must be emitted first");

        Assert.That(slang, Does.Not.Contain("sdCircle"),
            "SdCircle is never called by the fixture shader and must be pruned");
    }

    [Test]
    public void RecursiveCallProducesLocatedDiagnosticInsteadOfHanging()
    {
        const string source = """
                               using Rin.Shade;

                               namespace RecursionCheck;

                               public static class Recursive
                               {
                                   public static float PingPong(float x) => Pong(x);
                                   public static float Pong(float x) => Recursive.PingPong(x);
                               }

                               [Shader("Fixtures/recursion.slang")]
                               public class RecursionShader : Shader
                               {
                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       var x = Recursive.PingPong(1.0f);
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics.Any(d => d.Id == "SHADE0005"), Is.True);
    }

    [Test]
    public void SwizzleAccessEmitsLiteralLowercaseSwizzleText()
    {
        const string source = """
                               using System.Numerics;
                               using Rin.Shade;

                               namespace SwizzleCheck;

                               public struct SwizzlePush
                               {
                                   public Vector4 Color;
                                   public BufferRef<float> Output;
                               }

                               [Shader("Fixtures/swizzle.slang")]
                               public class SwizzleShader : Shader
                               {
                                   [Push] protected SwizzlePush Push;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       var rgb = Push.Color.rgb;
                                       Push.Output[0] = rgb.X;
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Shaders["SwizzleShader"], Does.Contain("var rgb = push.color.rgb;"));
    }

    [Test]
    public void RepeatedAndReorderedSwizzlesAreSupported()
    {
        const string source = """
                               using System.Numerics;
                               using Rin.Shade;

                               namespace SwizzleCheck;

                               public struct SwizzlePush
                               {
                                   public Vector4 Color;
                                   public BufferRef<float> Output;
                               }

                               [Shader("Fixtures/swizzle.slang")]
                               public class RepeatedSwizzleShader : Shader
                               {
                                   [Push] protected SwizzlePush Push;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       var broadcast = Push.Color.xxxx;
                                       var reversed = Push.Color.xyz.zyx;
                                       Push.Output[0] = broadcast.X + reversed.X;
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);
        var slang = result.Shaders["RepeatedSwizzleShader"];
        Assert.That(slang, Does.Contain("var broadcast = push.color.xxxx;"));
        Assert.That(slang, Does.Contain("var reversed = push.color.xyz.zyx;"));
    }
}
