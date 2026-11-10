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
            public static ReadTextures Textures;
            public static GlobalSamplers Samplers;

            public static Texture2D GetTexture(uint handle)
            {
                return Textures[(int)Shader.NonUniformResourceIndex(handle)];
            }
        }

        public struct OutputBindings
        {
            public RWByteAddressBuffer Output;
        }
        """;

    private static string Shader(string body) => Types + $$"""


        [Shader]
        public class BindlessShader : Shader
        {
            [BindingGroup] protected static OutputBindings Out;

            [Compute(1, 1, 1)]
            public void Compute()
            {
                {{body}}
            }
        }
        """;

    [Test]
    public void ReadingAStaticMemberDeclaresTheBlockAsANamedParameterBlock()
    {
        var result = ShadeEmitter.Emit(CompilationBuilder.Build(Shader(
            "Out.Output.Store(0u, BindlessData.Textures[1].Load(0, 0, 0).X);")));

        Assert.That(result.Diagnostics, Is.Empty);
        var slang = result.Shaders["BindlessShader"].Replace("\r\n", "\n");

        Assert.That(slang, Does.Contain("struct BindlessBlockAttribute { string name; };"));
        Assert.That(slang, Does.Contain(
            "[BindlessBlock(\"rin.global\")] ParameterBlock<BindlessData> rin_bindlessData;"));
        Assert.That(slang, Does.Contain("Texture2D textures[16];"));
        Assert.That(slang, Does.Contain("SamplerState samplers[4];"));
        Assert.That(slang, Does.Contain("rin_bindlessData.textures[1].Load(int3(0, 0, 0)).x"));
    }

    [Test]
    public void StaticMethodsOfTheBlockReadItsMembers()
    {
        var result = ShadeEmitter.Emit(CompilationBuilder.Build(Shader(
            "Out.Output.Store(0u, BindlessData.GetTexture(3u).Load(0, 0, 0).X);")));

        Assert.That(result.Diagnostics, Is.Empty);
        var slang = result.Shaders["BindlessShader"].Replace("\r\n", "\n");
        Assert.That(slang, Does.Contain("return rin_bindlessData.textures[(int)NonUniformResourceIndex(handle)];"));
        Assert.That(slang, Does.Contain("BindlessData.getTexture(3u).Load(int3(0, 0, 0)).x"));
    }

    [Test]
    public void ResourceMethodsMirrorTheSlangApiAndTakeANonUniformIndex()
    {
        var result = ShadeEmitter.Emit(CompilationBuilder.Build(Shader("""
            var uv = new System.Numerics.Vector2(0.5f, 0.25f);
                    var texture = BindlessData.Textures[(int)NonUniformResourceIndex(3u)];
                    Out.Output.Store(0u, texture.Sample(BindlessData.Samplers[1], uv).X);
                    Out.Output.Store(4u, texture.Load(2, 3, 0).Y);
                    uint width;
                    uint height;
                    uint levels;
                    texture.GetDimensions(0u, out width, out height, out levels);
                    Out.Output.Store(8u, width + height + levels);
            """)));

        Assert.That(result.Diagnostics, Is.Empty);
        var slang = result.Shaders["BindlessShader"].Replace("\r\n", "\n");
        Assert.That(slang, Does.Contain("Sample(rin_bindlessData.samplers[1], uv).x"));
        Assert.That(slang, Does.Contain(".Load(int3(2, 3, 0)).y"));
        Assert.That(slang, Does.Contain("NonUniformResourceIndex(3u)"));
        Assert.That(slang, Does.Contain(".GetDimensions(0u, width, height, levels);"));
    }

    [Test]
    public void ShaderThatReadsNoBlockDeclaresNoAttribute()
    {
        var result = ShadeEmitter.Emit(CompilationBuilder.Build(Shader("Out.Output.Store(0u, 1f);")));

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Shaders["BindlessShader"], Does.Not.Contain("BindlessBlock"));
    }

    [Test]
    public void BindlessBlockIsDeclaredBeforeOtherParameterBlocksSoItLandsAtSetZero()
    {
        var result = ShadeEmitter.Emit(CompilationBuilder.Build(Shader(
            "Out.Output.Store(0u, BindlessData.Textures[0].Load(0, 0, 0).X);")));

        Assert.That(result.Diagnostics, Is.Empty);
        var slang = result.Shaders["BindlessShader"];
        Assert.That(slang.IndexOf("ParameterBlock<BindlessData> rin_bindlessData", System.StringComparison.Ordinal),
            Is.LessThan(slang.IndexOf("ParameterBlock<OutputBindings> out", System.StringComparison.Ordinal)));
    }
}
