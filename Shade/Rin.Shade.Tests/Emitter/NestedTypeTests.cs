using Rin.Shade.Transpiler;

namespace Rin.Shade.Tests.Emitter;

public class NestedTypeTests
{
    private const string Source = """
        using Rin.Shade;

        namespace NestCheck;

        public struct Outer
        {
            public Inner Value;
            public Mode Mode;

            public struct Inner
            {
                public float X;
            }

            public enum Mode
            {
                A,
                B
            }
        }

        public class Holder
        {
            public struct Packed
            {
                public uint Bits;
            }
        }

        public struct NestPush
        {
            public Outer Outer;
            public Holder.Packed Packed;
            public BufferRef<float> Output;
        }

        [Shader("Fixtures/nested.slang")]
        public class NestShader : Shader
        {
            [Push] protected NestPush Push;

            [Compute(1, 1, 1)]
            public void Compute()
            {
                Push.Output[0] = Push.Outer.Value.X + (float)Push.Packed.Bits;
            }
        }
        """;

    private static string Emit()
    {
        var result = ShadeEmitter.Emit(CompilationBuilder.Build(Source));
        Assert.That(result.Diagnostics, Is.Empty);
        return result.Shaders["NestShader"].Replace("\r\n", "\n");
    }

    [Test]
    public void StructNestedInAStructIsDeclaredInsideItAndReferencedByPath()
    {
        var slang = Emit();

        Assert.That(slang, Does.Contain("""
                struct Outer
                {
                    Inner value;
                    Mode mode;

                    struct Inner
                    {
                        float x;
                    }
            """.Replace("\r\n", "\n")));
    }

    [Test]
    public void EnumNestedInAStructIsDeclaredInsideIt()
    {
        Assert.That(Emit(), Does.Contain("        enum Mode\n        {\n            A = 0,\n            B = 1\n        }"));
    }

    [Test]
    public void StructNestedInAClassReadsAsANamespaceOfTheClassName()
    {
        var slang = Emit();

        Assert.That(slang, Does.Contain("    namespace Holder\n    {\n        struct Packed"));
        Assert.That(slang, Does.Contain("Holder::Packed packed;"));
    }
}
