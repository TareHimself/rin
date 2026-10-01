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
}
