using Rin.Shade.Transpiler;

namespace Rin.Shade.Tests.Emitter;

public class SwitchScopeTests
{
    private const string Source = """
        using Rin.Shade;

        namespace SwitchScopeCheck;

        public struct SwitchPush
        {
            public int Mode;
            public float A;
            public BufferRef<float> Output;
        }

        [Shader("Fixtures/switch_scope.slang")]
        public class SwitchShader : Shader
        {
            [Push] protected SwitchPush Push;

            [Compute(1, 1, 1)]
            public void Compute()
            {
                switch (Push.Mode)
                {
                    case 0:
                    {
                        var value = Push.A * 2f;
                        Push.Output[0] = value;
                        break;
                    }

                    case 1:
                    {
                        var value = Push.A * 3f;
                        Push.Output[0] = value;
                        break;
                    }
                }
            }
        }
        """;

    [Test]
    public void EachCaseBodyKeepsItsOwnScope()
    {
        var result = ShadeEmitter.Emit(CompilationBuilder.Build(Source));

        Assert.That(result.Diagnostics, Is.Empty);
        var slang = result.Shaders["SwitchShader"].Replace("\r\n", "\n");
        Assert.That(slang, Does.Contain("case 0:\n        {\n            var value = push.a * 2;"));
        Assert.That(slang, Does.Contain("case 1:\n        {\n            var value = push.a * 3;"));
    }
}
