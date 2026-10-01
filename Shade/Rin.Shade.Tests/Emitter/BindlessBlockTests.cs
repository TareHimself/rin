using System.Linq;
using Rin.Shade.Transpiler;

namespace Rin.Shade.Tests.Emitter;

public class BindlessBlockTests
{
    private const string Types = """
        using System.Runtime.CompilerServices;
        using Rin.Shade;

        namespace BindlessCheck;

        [InlineArray(16)]
        public struct ReadTextures
        {
            private Texture2D _element;
        }

        [InlineArray(4)]
        public struct GlobalSamplers
        {
            private SamplerState _element;
        }

        [BindlessBlock("rin.global")]
        public struct BindlessData
        {
            public ReadTextures Textures;
            public GlobalSamplers Samplers;
        }

        public struct BindlessPush
        {
            public BufferRef<float> Output;
        }
        """;

    private static string Shader(string field) => Types + $$"""


        [Shader("Fixtures/bindless.slang")]
        public class BindlessShader : Shader
        {
            [Push] protected BindlessPush Push;
            {{field}}

            [Compute(1, 1, 1)]
            public void Compute()
            {
                Push.Output[0] = 1f;
            }
        }
        """;

    [Test]
    public void FieldOfABindlessBlockTypeLowersToANamedParameterBlock()
    {
        var result = ShadeEmitter.Emit(CompilationBuilder.Build(Shader("protected static BindlessData Bindless;")));

        Assert.That(result.Diagnostics, Is.Empty);
        var slang = result.Shaders["BindlessShader"].Replace("\r\n", "\n");

        Assert.That(slang, Does.Contain("struct BindlessBlockAttribute { string name; };"));
        Assert.That(slang, Does.Contain(
            "[BindlessBlock(\"rin.global\")] ParameterBlock<BindlessCheck::BindlessData> bindless;"));
        Assert.That(slang, Does.Contain("Texture2D textures[16];"));
        Assert.That(slang, Does.Contain("SamplerState samplers[4];"));
    }

    [Test]
    public void ShaderWithoutABindlessBlockDeclaresNoAttribute()
    {
        var result = ShadeEmitter.Emit(CompilationBuilder.Build(Shader("")));

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Shaders["BindlessShader"], Does.Not.Contain("BindlessBlock"));
    }

    [Test]
    public void InstanceFieldOfABindlessBlockTypeIsRejected()
    {
        var result = ShadeEmitter.Emit(CompilationBuilder.Build(Shader("protected BindlessData Bindless;")));

        Assert.That(result.Diagnostics.Any(d => d.Id == Diagnostics.Emitter.UnsupportedBindingField.Id), Is.True);
    }

    [Test]
    public void BindlessBlockIsDeclaredBeforeOtherParameterBlocksSoItLandsAtSetZero()
    {
        const string material = """
            public struct MaterialResources
            {
                public ReadTextures Maps;
            }
            """;

        var source = Types + "\n" + material + """


            [Shader("Fixtures/bindless_order.slang")]
            public class OrderShader : Shader
            {
                [Push] protected BindlessPush Push;
                [BindingGroup] protected static MaterialResources Material;
                protected static BindlessData Bindless;

                [Compute(1, 1, 1)]
                public void Compute()
                {
                    Push.Output[0] = 1f;
                }
            }
            """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);
        var slang = result.Shaders["OrderShader"];
        Assert.That(slang.IndexOf("ParameterBlock<BindlessCheck::BindlessData> bindless", System.StringComparison.Ordinal),
            Is.LessThan(slang.IndexOf("ParameterBlock<BindlessCheck::MaterialResources> material", System.StringComparison.Ordinal)));
    }
}
