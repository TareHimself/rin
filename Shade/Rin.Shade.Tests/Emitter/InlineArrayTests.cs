using System.Linq;
using Rin.Shade.Transpiler;

namespace Rin.Shade.Tests.Emitter;

public class InlineArrayTests
{
    [Test]
    public void InlineArrayLocalsLowerToSizedSlangArrays()
    {
        const string source = """
                               using System.Runtime.CompilerServices;
                               using Rin.Shade;

                               namespace InlineArrayLocalCheck;

                               [InlineArray(4)]
                               public struct FourFloats
                               {
                                   private float _element;
                               }

                               public struct LocalPush
                               {
                                   public BufferRef<float> Output;
                               }

                               [Shader("Fixtures/inline_array_local.slang")]
                               public class InlineArrayLocalShader : Shader
                               {
                                   [Push] protected LocalPush Push;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       FourFloats a = default;
                                       var b = new FourFloats();
                                       a[0] = 1f;
                                       FourFloats c = a;
                                       b[1] = c[0];
                                       Push.Output[0] = b[1];
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);
        var slang = result.Shaders["InlineArrayLocalShader"];
        Assert.That(slang, Does.Contain("float a[4] = {};"));
        Assert.That(slang, Does.Contain("float b[4] = {};"));
        Assert.That(slang, Does.Contain("float c[4] = a;"));
        Assert.That(slang, Does.Contain("b[1] = c[0];"));
    }

    private static string ArrayShader(string body) => $$"""
        using Rin.Shade;

        namespace ArrayCreationCheck;

        public struct ArrayPush
        {
            public BufferRef<float> Output;
            public int Length;
        }

        [Shader("Fixtures/array_creation.slang")]
        public class ArrayCreationShader : Shader
        {
            private const int Count = 3;

            [Push] protected ArrayPush Push;

            [Compute(1, 1, 1)]
            public void Compute()
            {
                {{body}}
            }
        }
        """;

    [Test]
    public void NewArrayWithConstantLengthLowersToSizedSlangArray()
    {
        var source = ArrayShader("""
            var a = new float[4];
            float[] b = new float[Count];
            var c = new float[] { 1f, 2f };
            a[0] = c[1];
            Push.Output[0] = a[0] + b[2];
            """);

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);
        var slang = result.Shaders["ArrayCreationShader"];
        Assert.That(slang, Does.Contain("float a[4] = {};"));
        Assert.That(slang, Does.Contain("float b[3] = {};"));
        Assert.That(slang, Does.Contain("float c[2] = { 1, 2 };"));
        Assert.That(slang, Does.Contain("a[0] = c[1];"));
    }

    [Test]
    public void NewArrayWithRuntimeLengthIsRejected()
    {
        var source = ArrayShader("""
            var a = new float[Push.Length];
            Push.Output[0] = a[0];
            """);

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics.Any(d => d.Id == Diagnostics.Emitter.UnsupportedType.Id), Is.True);
    }
}
