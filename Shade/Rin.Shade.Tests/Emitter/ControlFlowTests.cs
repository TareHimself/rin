namespace Rin.Shade.Tests.Emitter;

using Rin.Shade.Transpiler;

public class ControlFlowTests
{
    [Test]
    public void EmitsForWhileSwitchFixedForeachAndInlinedLocalFunction()
    {
        var source = FixtureSource.Read("../Fixtures/ControlFlowFixtureShader.cs");
        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);
        var slang = result.Shaders["ControlFlowFixtureShader"];

        Assert.That(slang, Does.Contain("for (var i = 0; i < 4; i++)"));
        Assert.That(slang, Does.Contain("continue;"));

        Assert.That(slang, Does.Contain("while (j < (int)push.count)"));
        Assert.That(slang, Does.Contain("break;"));

        Assert.That(slang, Does.Contain("switch ((int)push.count)"));
        Assert.That(slang, Does.Contain("case 0:"));
        Assert.That(slang, Does.Contain("default:"));

        // foreach over a fixed-size array desugars to an index loop, not a diagnostic.
        Assert.That(slang, Does.Contain("var weight = push.weights.values[i0];"));

        // Local function is inlined: nothing at the declaration site, emitted as its own function,
        // called by name at the call site.
        Assert.That(slang, Does.Not.Contain("Double"));
        Assert.That(slang, Does.Contain("float double(float x)"));
        Assert.That(slang, Does.Contain("sum = double(sum);"));
    }

    [Test]
    public void IfElseChainsKeepEveryBranch()
    {
        const string source = """
            using Rin.Shade;
            using Rin.Shade.Tests;

            namespace IfElseCheck;

            public struct IfElsePush
            {
                public float Value;
                public TestBuffer<float> Output;
            }

            [Shader]
            public class IfElseShader : Shader
            {
                [Push] protected IfElsePush Push;

                [Compute(1, 1, 1)]
                public void Compute()
                {
                    if (Push.Value < 0f)
                    {
                        Push.Output[0] = -1f;
                    }
                    else if (Push.Value > 0f)
                    {
                        Push.Output[0] = 1f;
                    }
                    else
                    {
                        Push.Output[0] = 0f;
                    }
                }
            }
            """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);
        var slang = result.Shaders["IfElseShader"].Replace("\r\n", "\n");
        Assert.That(slang, Does.Contain(
            "    if (push.value < 0)\n" +
            "    {\n" +
            "        push.output.setItem(0, -1);\n" +
            "    }\n" +
            "    else\n" +
            "    {\n" +
            "        if (push.value > 0)\n" +
            "        {\n" +
            "            push.output.setItem(0, 1);\n" +
            "        }\n" +
            "        else\n" +
            "        {\n" +
            "            push.output.setItem(0, 0);\n" +
            "        }\n" +
            "    }\n"));
    }
}
