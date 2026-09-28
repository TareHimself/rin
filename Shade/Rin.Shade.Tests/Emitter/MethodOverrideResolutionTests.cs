using System.Linq;
using Rin.Shade.Transpiler;

namespace Rin.Shade.Tests.Emitter;

// A call to an abstract/virtual method from a shared base class resolves, in Roslyn's operation
// tree, to the base's own declaration - not the override that runs. OverrideResolution fixes that.
public class MethodOverrideResolutionTests
{
    [Test]
    public void CallToOverriddenHelperResolvesToMostDerivedImplementation()
    {
        const string source = """
                               using Rin.Shade;

                               namespace OverrideResolutionCheck;

                               public struct ConsumerPush
                               {
                                   public float Value;
                                   public BufferRef<float> Output;
                               }

                               public abstract class BaseShader : Shader
                               {
                                   [Push] protected ConsumerPush Push;

                                   protected abstract float GetValue(float raw);

                                   [Compute(1, 1, 1)]
                                   public virtual void Compute()
                                   {
                                       Push.Output[0] = GetValue(Push.Value);
                                   }
                               }

                               [Shader("Fixtures/override_resolution.slang")]
                               public class ConsumerShader : BaseShader
                               {
                                   protected override float GetValue(float raw) => raw * 2f;
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);

        const string expected = """
                                 struct ConsumerPush
                                 {
                                     float value;
                                     float* output;
                                 }

                                 float getValue(float raw)
                                 {
                                     return raw * 2;
                                 }

                                 [[vk::push_constant]] uniform ConstantBuffer<ConsumerPush, ScalarDataLayout> push;

                                 [shader("compute")]
                                 [numthreads(1, 1, 1)]
                                 void compute()
                                 {
                                     push.output[0] = getValue(push.value);
                                 }

                                 """;

        Assert.That(result.Shaders["ConsumerShader"].Replace("\r\n", "\n"),
            Is.EqualTo(expected.Replace("\r\n", "\n")));
    }

    // No override anywhere in the chain still means no body - stays a diagnostic, not silently
    // wrong output, matching every other "no source for method" case.
    [Test]
    public void UnresolvedAbstractMethodStillProducesDiagnostic()
    {
        const string source = """
                               using Rin.Shade;

                               namespace UnresolvedAbstractCheck;

                               [Shader("Fixtures/unresolved_abstract.slang")]
                               public abstract class BaseShader : Shader
                               {
                                   protected abstract float GetValue();

                                   [Compute(1, 1, 1)]
                                   public virtual void Compute()
                                   {
                                       var v = GetValue();
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics.Any(d => d.Id == "SHADE0004"), Is.True);
    }
}
