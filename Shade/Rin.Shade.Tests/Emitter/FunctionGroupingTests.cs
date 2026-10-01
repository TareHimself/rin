using Rin.Shade.Transpiler;

namespace Rin.Shade.Tests.Emitter;

public class FunctionGroupingTests
{
    private const string Source = """
        using Rin.Shade;

        namespace FunctionGroupingCheck;

        public struct Counter
        {
            public float Value;

            public float Get() => Value;
            public float Twice() => Get() * 2f;
        }

        public struct CounterPush
        {
            public Counter Counter;
            public BufferRef<float> Output;
        }

        [Shader("Fixtures/function_grouping.slang")]
        public class CounterShader : Shader
        {
            [Push] protected CounterPush Push;

            private static float Helper(float x) => x * 3f;

            [Compute(1, 1, 1)]
            public void Compute()
            {
                Push.Output[0] = Push.Counter.Get() + Helper(Push.Counter.Value) + Push.Counter.Twice();
            }
        }
        """;

    [Test]
    public void MembersOfOneStructShareAnExtensionBlockWhenCallsAllowIt()
    {
        var result = ShadeEmitter.Emit(CompilationBuilder.Build(Source));

        Assert.That(result.Diagnostics, Is.Empty);
        var slang = result.Shaders["CounterShader"];
        Assert.That(slang.Split("extension ").Length - 1, Is.EqualTo(1));
        var start = slang.IndexOf("extension ", System.StringComparison.Ordinal);
        var end = slang.IndexOf("\n}\n", start, System.StringComparison.Ordinal);
        var block = slang[start..end];
        Assert.That(block, Does.Contain("float get()"));
        Assert.That(block, Does.Contain("float twice()"));
    }
}
