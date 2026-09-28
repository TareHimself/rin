using System.Linq;
using Rin.Shade.Transpiler;

namespace Rin.Shade.Tests.Emitter;

public class ShaderBindingTests
{
    [Test]
    public void ShaderBindingFieldsLowerToModuleScopeDeclarations()
    {
        const string source = """
                               using Rin.Shade;

                               namespace ShaderBindingCheck;

                               public struct BindingPush
                               {
                                   public int Index;
                                   public BufferRef<float> Output;
                               }

                               [Shader("Fixtures/binding.slang")]
                               public class BindingShader : Shader
                               {
                                   [ShaderBinding(Set = 0, Binding = 0)]
                                   protected static readonly SamplerState[] Samplers = new SamplerState[4];

                                   [ShaderBinding(Set = 0, Binding = 1)]
                                   protected static readonly Texture2D[] ReadTextures = new Texture2D[8];

                                   [Push] protected BindingPush Push;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       var t = ReadTextures[Push.Index];
                                       Push.Output[0] = 1f;
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);

        const string expected = """
                                 struct BindingPush
                                 {
                                     int index;
                                     float* output;
                                 }

                                 [[vk::binding(0, 0)]] uniform SamplerState samplers[4];
                                 [[vk::binding(1, 0)]] uniform Texture2D readTextures[8];

                                 [[vk::push_constant]] uniform ConstantBuffer<BindingPush, ScalarDataLayout> push;

                                 [shader("compute")]
                                 [numthreads(1, 1, 1)]
                                 void compute()
                                 {
                                     var t = readTextures[push.index];
                                     push.output[0] = 1;
                                 }

                                 """;

        Assert.That(result.Shaders["BindingShader"].Replace("\r\n", "\n"),
            Is.EqualTo(expected.Replace("\r\n", "\n")));
    }

    [Test]
    public void DuplicateBindingSlotIsRejected()
    {
        const string source = """
                               using Rin.Shade;

                               namespace DuplicateBindingCheck;

                               [Shader("Fixtures/dup_binding.slang")]
                               public class DupBindingShader : Shader
                               {
                                   [ShaderBinding(Set = 0, Binding = 0)]
                                   protected static readonly Texture2D[] A = new Texture2D[4];

                                   [ShaderBinding(Set = 0, Binding = 0)]
                                   protected static readonly Texture2D[] B = new Texture2D[4];

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics.Any(d => d.Id == "SHADE0014"), Is.True);
    }

    [Test]
    public void NonResourceBindingFieldIsRejected()
    {
        const string source = """
                               using Rin.Shade;

                               namespace NonResourceBindingCheck;

                               [Shader("Fixtures/bad_binding.slang")]
                               public class BadBindingShader : Shader
                               {
                                   [ShaderBinding(Set = 0, Binding = 0)]
                                   protected static readonly float[] Bad = new float[4];

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics.Any(d => d.Id == "SHADE0013"), Is.True);
    }
}
