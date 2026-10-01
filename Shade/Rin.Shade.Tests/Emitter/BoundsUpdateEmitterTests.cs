using System.Linq;
using Rin.Shade.Transpiler;

namespace Rin.Shade.Tests.Emitter;

// Port of Shaders/World/Mesh/Compute/bounds_update.slang - see Fixtures/BoundsUpdateFixtureShader.cs.
// Expected output was verified to actually compile with the real Slang compiler (rin-slang compile).
public class BoundsUpdateEmitterTests
{
    [Test]
    public void EmitsBoundsUpdateFixtureShader()
    {
        var source = FixtureSource.Read("../Fixtures/BoundsUpdateFixtureShader.cs");
        var compilation = CompilationBuilder.Build(source);

        var result = ShadeEmitter.Emit(compilation);

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Shaders.Keys, Is.EquivalentTo(new[] { "BoundsUpdateFixtureShader" }));

        Snapshot.Verify(result.Shaders["BoundsUpdateFixtureShader"]);
    }
}
