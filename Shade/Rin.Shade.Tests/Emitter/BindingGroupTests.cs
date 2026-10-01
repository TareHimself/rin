using System.Linq;
using Rin.Shade.Transpiler;

namespace Rin.Shade.Tests.Emitter;

public class BindingGroupTests
{
    [Test]
    public void BindingGroupFieldLowersToParameterBlock()
    {
        const string source = """
                               using System.Runtime.CompilerServices;
                               using Rin.Shade;

                               namespace BindingGroupCheck;

                               [InlineArray(4)]
                               public struct FourSamplers
                               {
                                   private SamplerState _element;
                               }

                               [InlineArray(8)]
                               public struct EightTextures
                               {
                                   private Texture2D _element;
                               }

                               [ShaderStruct]
                               public struct MaterialResources
                               {
                                   public FourSamplers Samplers;
                                   public EightTextures ReadTextures;
                               }

                               public struct BindingPush
                               {
                                   public int Index;
                                   public BufferRef<float> Output;
                               }

                               [Shader("Fixtures/binding.slang")]
                               public class BindingShader : Shader
                               {
                                   [BindingGroup]
                                   protected static MaterialResources Material;

                                   [Push] protected BindingPush Push;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       var t = Material.ReadTextures[Push.Index];
                                       Push.Output[0] = 1f;
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);

        const string expected = """
                                 namespace BindingGroupCheck
                                 {
                                     struct BindingPush
                                     {
                                         int index;
                                         float* output;
                                     }

                                     struct MaterialResources
                                     {
                                         SamplerState samplers[4];
                                         Texture2D readTextures[8];
                                     }

                                 }

                                 ParameterBlock<BindingGroupCheck::MaterialResources> material;

                                 [[vk::push_constant]] uniform ConstantBuffer<BindingGroupCheck::BindingPush, ScalarDataLayout> push;

                                 [shader("compute")]
                                 [numthreads(1, 1, 1)]
                                 void compute()
                                 {
                                     var t = material.readTextures[push.index];
                                     push.output[0] = 1;
                                 }

                                 """;

        Assert.That(result.Shaders["BindingShader"].Replace("\r\n", "\n"),
            Is.EqualTo(expected.Replace("\r\n", "\n")));
    }

    [Test]
    public void NonStructBindingGroupFieldIsRejected()
    {
        const string source = """
                               using Rin.Shade;

                               namespace BadBindingGroupCheck;

                               [Shader("Fixtures/bad_binding.slang")]
                               public class BadBindingShader : Shader
                               {
                                   [BindingGroup]
                                   protected static float Bad;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics.Any(d => d.Id == Diagnostics.Emitter.UnsupportedBindingField.Id), Is.True);
    }

    [Test]
    public void NonStaticBindingGroupFieldIsRejected()
    {
        const string source = """
                               using System.Runtime.CompilerServices;
                               using Rin.Shade;

                               namespace InstanceBindingGroupCheck;

                               [InlineArray(1)]
                               public struct OneTexture
                               {
                                   private Texture2D _element;
                               }

                               [ShaderStruct]
                               public struct MaterialResources
                               {
                                   public OneTexture ReadTextures;
                               }

                               [Shader("Fixtures/bad_instance_binding.slang")]
                               public class BadInstanceBindingShader : Shader
                               {
                                   [BindingGroup]
                                   protected MaterialResources Material;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics.Any(d => d.Id == Diagnostics.Emitter.UnsupportedBindingField.Id), Is.True);
    }
}
