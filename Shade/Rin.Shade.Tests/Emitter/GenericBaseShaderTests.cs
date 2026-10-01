using Rin.Shade.Transpiler;

namespace Rin.Shade.Tests.Emitter;

// A closed generic base ("class Consumer : Base<ConsumerData>") is a cheap, interface-free way to
// share an algorithm across data structs - TData gets substituted for free, and OverrideResolution
// resolves the abstract helper call to Consumer's override.
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

        Snapshot.Verify(result.Shaders["ConsumerShader"]);
    }
}
