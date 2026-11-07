using Rin.Shade.Transpiler;

namespace Rin.Shade.Tests.Emitter;

/// <summary>
/// Slang reads an unsuffixed integer literal as int, or as int64 when it does not fit, so unsigned and 64-bit
/// literals and constants keep their suffix.
/// </summary>
public class LiteralSuffixTests
{
    private const string Source = """
        using Rin.Shade;

        namespace LiteralCheck;

        public static class Masks
        {
            public const uint All = 0xFFFFFFFFu;
        }

        public struct LiteralPush
        {
            public uint Shift;
            public BufferRef<uint> Output;
            public BufferRef<long> Wide;
        }

        [Shader]
        public class LiteralShader : Shader
        {
            [Push] protected LiteralPush Push;

            [Compute(1, 1, 1)]
            public void Compute()
            {
                Push.Output[0] = 0xFFFFFFFFu >> (int)Push.Shift;
                Push.Output[1] = Masks.All;
                Push.Output[2] = 7u + (uint)3;
                Push.Wide[0] = 5000000000L;
            }
        }
        """;

    [Test]
    public void UnsignedAndLongLiteralsKeepTheirSuffix()
    {
        var result = ShadeEmitter.Emit(CompilationBuilder.Build(Source));
        Assert.That(result.Diagnostics, Is.Empty);
        var slang = result.Shaders["LiteralShader"];

        Assert.Multiple(() =>
        {
            Assert.That(slang, Does.Contain("4294967295u >> (int)push.shift"));
            Assert.That(slang, Does.Contain("push.output[1] = 4294967295u;"));
            Assert.That(slang, Does.Contain("7u + (uint)3"));
            Assert.That(slang, Does.Contain("5000000000ll"));
        });
    }
}
