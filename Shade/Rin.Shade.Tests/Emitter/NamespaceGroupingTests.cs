using Rin.Shade.Transpiler;

namespace Rin.Shade.Tests.Emitter;

public class NamespaceGroupingTests
{
    private const string Source = """
        using Rin.Shade;

        namespace Alpha
        {
            public struct First { public float Value; }
            public struct Third { public float Value; }
        }

        namespace Beta
        {
            public struct Second { public float Value; }
        }

        namespace GroupingCheck
        {
            public struct GroupingPush
            {
                public Alpha.First A;
                public Beta.Second B;
                public Alpha.Third C;
                public BufferRef<float> Output;
            }

            [Shader("Fixtures/grouping.slang")]
            public class GroupingShader : Shader
            {
                [Push] protected GroupingPush Push;

                [Compute(1, 1, 1)]
                public void Compute()
                {
                    Push.Output[0] = Push.A.Value + Push.B.Value + Push.C.Value;
                }
            }
        }
        """;

    [Test]
    public void TypesOfOneNamespaceShareABlockWhenDependenciesAllowIt()
    {
        var result = ShadeEmitter.Emit(CompilationBuilder.Build(Source));

        Assert.That(result.Diagnostics, Is.Empty);
        var slang = result.Shaders["GroupingShader"];
        var alphaBlocks = slang.Split("namespace Alpha").Length - 1;
        Assert.That(alphaBlocks, Is.EqualTo(1));
        Assert.That(slang.IndexOf("struct First"), Is.LessThan(slang.IndexOf("struct Third")));
        Assert.That(slang.IndexOf("struct Third"), Is.LessThan(slang.IndexOf("struct Second")));
    }
}
