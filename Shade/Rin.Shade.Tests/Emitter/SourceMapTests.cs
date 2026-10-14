using Rin.Shade.Transpiler;

namespace Rin.Shade.Tests.Emitter;

public class SourceMapTests
{
    private const string Source = """
        using Rin.Shade;

        namespace SourceMapCheck;

        public struct MapPush
        {
            public float A;
            public BufferRef<float> Output;
        }

        [Shader("Fixtures/source_map.slang")]
        public class MapShader : Shader
        {
            [Push] protected MapPush Push;

            private static float Twice(float x)
            {
                return x * 2f;
            }

            [Compute(1, 1, 1)]
            public void Compute()
            {
                var doubled = Twice(Push.A);
                Push.Output[0] = doubled + 1f;
            }
        }
        """;

    [Test]
    public void Statement_lines_map_back_to_their_csharp_lines()
    {
        var result = ShadeEmitter.Emit(CompilationBuilder.Build(Source));
        var slang = result.Shaders["MapShader"];
        var map = result.SourceMaps["MapShader"];

        Assert.That(CsharpLineOf(map, LineOf(slang, "var doubled = twice(push.a);")),
            Is.EqualTo(LineOf(Source, "var doubled = Twice(Push.A);")));
        Assert.That(CsharpLineOf(map, LineOf(slang, "return x * 2;")),
            Is.EqualTo(LineOf(Source, "return x * 2f;")));
    }

    [Test]
    public void Signature_lines_map_to_the_method_and_type_lines_map_nowhere()
    {
        var result = ShadeEmitter.Emit(CompilationBuilder.Build(Source));
        var slang = result.Shaders["MapShader"];
        var map = result.SourceMaps["MapShader"];

        Assert.That(CsharpLineOf(map, LineOf(slang, "float twice(float x)")),
            Is.EqualTo(LineOf(Source, "private static float Twice(float x)")));
        Assert.That(map.Origin(LineOf(slang, "struct MapPush")), Is.Null);
        Assert.That(map.LineCount, Is.EqualTo(slang.Count(c => c == '\n')));
    }

    private static int LineOf(string text, string needle)
    {
        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
            if (lines[i].Contains(needle))
                return i + 1;
        throw new AssertionException($"'{needle}' not found in:\n{text}");
    }

    private static int CsharpLineOf(SlangSourceMap map, int slangLine)
    {
        var origin = map.Origin(slangLine);
        Assert.That(origin, Is.Not.Null, $"Slang line {slangLine} has no origin");
        return origin!.GetLineSpan().StartLinePosition.Line + 1;
    }
}
