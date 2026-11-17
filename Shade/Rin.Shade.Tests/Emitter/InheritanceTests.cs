namespace Rin.Shade.Tests.Emitter;

using Rin.Shade.Transpiler;

public class InheritanceTests
{
    [Test]
    public void OnlyTheShaderAttributedClassIsEmittedWithItsInheritedMembersFlattened()
    {
        var source = FixtureSource.Read("../Fixtures/InheritanceFixtureShaders.cs");
        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Shaders.Keys, Is.EquivalentTo(new[] { "DerivedInheritanceShader" }));

        var slang = result.Shaders["DerivedInheritanceShader"];

        // [Push] is declared on the base class only - still has to be found and lowered.
        Assert.That(slang, Does.Contain("InheritancePushConstants, ScalarDataLayout> push;"));

        // The derived class's OWN override body is what gets used, not the base's.
        Assert.That(slang, Does.Contain("push.output.setItem(0, push.value + 1);"));
    }
}
