using System.Linq;
using Rin.Shade.Transpiler;

namespace Rin.Shade.Tests.Emitter;

public class IndexerTests
{
    private static string Source(string body) => $$"""
        using System.Numerics;
        using System.Runtime.CompilerServices;
        using Rin.Shade;
        using Rin.Shade.Tests;

        namespace IndexerCheck;

        public struct IndexerPush
        {
            public TestBuffer<Vector3> Points;
            public TestBuffer<int> Sizes;
        }

        [Shader]
        public class IndexerShader : Shader
        {
            [Push] protected IndexerPush Push;

            [Compute(1, 1, 1)]
            public void Compute()
            {
                {{body}}
            }
        }
        """;

    [Test]
    public void ReadsAndWritesCallTheAccessorsWithTheirArguments()
    {
        var result = ShadeEmitter.Emit(CompilationBuilder.Build(Source("Push.Points[2u] = Push.Points[1u] * 2f;")));

        Assert.That(result.Diagnostics, Is.Empty);
        var slang = result.Shaders["IndexerShader"];
        Assert.That(slang, Does.Contain("push.points.setItem(2u, push.points.getItem(1u) * 2);"));
        Assert.That(slang, Does.Contain("void setItem(uint index, float3 value)"));
        Assert.That(slang, Does.Not.Contain("[mutating]"));
    }

    [Test]
    public void SizeOfFoldsToTheScalarLayoutSize()
    {
        var result = ShadeEmitter.Emit(CompilationBuilder.Build(Source(
            "Push.Sizes[0] = Unsafe.SizeOf<Vector3>() + Unsafe.SizeOf<Matrix4x4>();")));

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Shaders["IndexerShader"], Does.Contain("push.sizes.setItem(0, 12 + 64);"));
    }

    [Test]
    public void CompoundAssignmentOfAnIndexerIsRejected()
    {
        var result = ShadeEmitter.Emit(CompilationBuilder.Build(Source("Push.Sizes[0] += 1;")));

        Assert.That(result.Diagnostics.Any(d => d.Id == Diagnostics.Emitter.UnsupportedConstruct.Id), Is.True);
    }
}
