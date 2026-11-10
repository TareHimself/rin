namespace Rin.Shade.Tests.Emitter;

using Rin.Shade.Transpiler;

public class IntrinsicsTests
{
    [Test]
    public void DiscardEmitsAsAStatementNotAnExpression()
    {
        const string source = """
                               using Rin.Shade;
                               using Rin.Shade.Tests;

                               namespace DiscardCheck;

                               public struct DiscardPush
                               {
                                   public float Value;
                                   public TestBuffer<float> Output;
                               }

                               [Shader]
                               public class DiscardShader : Shader
                               {
                                   [Push] protected DiscardPush Push;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       if (Push.Value < 0f)
                                       {
                                           Discard();
                                       }

                                       Push.Output[0] = Push.Value;
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);
        var slang = result.Shaders["DiscardShader"];
        Assert.That(slang, Does.Contain("discard;"));
        Assert.That(slang, Does.Not.Contain("discard;;"));
    }

    [Test]
    public void ReinterpretSubstitutesTypeArgument()
    {
        const string source = """
                               using Rin.Shade;
                               using Rin.Shade.Tests;

                               namespace ReinterpretCheck;

                               public struct ReinterpretPush
                               {
                                   public float Value;
                                   public TestBuffer<int> Output;
                               }

                               [Shader]
                               public class ReinterpretShader : Shader
                               {
                                   [Push] protected ReinterpretPush Push;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       var reinterpreted = Push.Value.Reinterpret<int>();
                                       Push.Output[0] = reinterpreted;
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);
        var slang = result.Shaders["ReinterpretShader"];
        Assert.That(slang, Does.Contain("var reinterpreted = reinterpret<int>(push.value);"));
    }
}
