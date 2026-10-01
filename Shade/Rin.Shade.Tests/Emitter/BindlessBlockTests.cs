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
            "[BindlessBlock(\"rin.global\")] ParameterBlock<BindlessData> bindless;"));
        Assert.That(slang, Does.Contain("Texture2D textures[16];"));
        Assert.That(slang, Does.Contain("SamplerState samplers[4];"));
    }

    [Test]
    public void ResourceMethodsMirrorTheSlangApiAndTakeANonUniformIndex()
    {
        var source = Types + """


            [Shader("Fixtures/bindless_sample.slang")]
            public class SampleShader : Shader
            {
                [Push] protected BindlessPush Push;
                protected static BindlessData Bindless;

                [Compute(1, 1, 1)]
                public void Compute()
                {
                    var uv = new System.Numerics.Vector2(0.5f, 0.25f);
                    var texture = Bindless.Textures[(int)NonUniformResourceIndex(3u)];
                    Push.Output[0] = texture.Sample(Bindless.Samplers[1], uv).X;
                    Push.Output[1] = texture.Load(2, 3, 0).Y;
                    uint width;
                    uint height;
                    uint levels;
                    texture.GetDimensions(0u, out width, out height, out levels);
                    Push.Output[2] = width + height + levels;
                }
            }
            """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);
        var slang = result.Shaders["SampleShader"].Replace("\r\n", "\n");
        Assert.That(slang, Does.Contain("Sample(bindless.samplers[1], uv).x"));
        Assert.That(slang, Does.Contain(".Load(int3(2, 3, 0)).y"));
        Assert.That(slang, Does.Contain("NonUniformResourceIndex(3)"));
        Assert.That(slang, Does.Contain(".GetDimensions(0, width, height, levels);"));
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
        Assert.That(slang.IndexOf("ParameterBlock<BindlessData> bindless", System.StringComparison.Ordinal),
            Is.LessThan(slang.IndexOf("ParameterBlock<MaterialResources> material", System.StringComparison.Ordinal)));
    }
}
