using Rin.Shade.Transpiler;

namespace Rin.Shade.Tests.Emitter;

// A shader class deriving from a CLOSED generic base ("class Consumer : Base<ConsumerData>") is a
// cheap, interface-free way to share an algorithm across data structs. Roslyn substitutes TData
// throughout the base's members for free (chain's GetMembers() already returns it as
// ConsumerData), and OverrideResolution.cs resolves a call to an abstract/virtual helper - made
// from code physically declared in the base - to the concrete override.
public class GenericBaseShaderTests
{
    [Test]
    public void ShaderDerivingFromClosedGenericBaseCallsIntoOverriddenAbstractHelper()
    {
        const string source = """
                               using Rin.Shade;

                               namespace GenericBaseCheck;

                               public struct ConsumerPush
                               {
                                   public float Value;
                                   public BufferRef<float> Output;
                               }

                               public abstract class GenericBaseShader<TData> : Shader
                               {
                                   [Push] protected TData Push;

                                   protected abstract float GetValue(TData push);

                                   [Compute(1, 1, 1)]
                                   public virtual void Compute()
                                   {
                                       var v = GetValue(Push);
                                   }
                               }

                               [Shader("Fixtures/generic_base.slang")]
                               public class ConsumerShader : GenericBaseShader<ConsumerPush>
                               {
                                   protected override float GetValue(ConsumerPush push) => push.Value;
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

                                 float getValue(ConsumerPush push)
                                 {
                                     return push.value;
                                 }

                                 [[vk::push_constant]] uniform ConstantBuffer<ConsumerPush, ScalarDataLayout> push;

                                 [shader("compute")]
                                 [numthreads(1, 1, 1)]
                                 void compute()
                                 {
                                     var v = getValue(push);
                                 }

                                 """;

        Assert.That(result.Shaders["ConsumerShader"].Replace("\r\n", "\n"),
            Is.EqualTo(expected.Replace("\r\n", "\n")));
    }
}
