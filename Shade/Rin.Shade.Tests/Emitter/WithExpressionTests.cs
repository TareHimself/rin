using System.Linq;
using Rin.Shade.Transpiler;

namespace Rin.Shade.Tests.Emitter;

public class WithExpressionTests
{
    [Test]
    public void WithExpressionLowersToSynthesizedHelperCall()
    {
        const string source = """
                               using System.Numerics;
                               using Rin.Shade;

                               namespace WithExpressionCheck;

                               [ShaderStruct]
                               public struct Bounds3D
                               {
                                   public Vector3 Lower;
                                   public Vector3 Upper;
                               }

                               public struct WithPush
                               {
                                   public Bounds3D Bounds;
                                   public Vector3 NewUpper;
                                   public BufferRef<Bounds3D> Output;
                               }

                               [Shader("Fixtures/with_expression.slang")]
                               public class WithExpressionShader : Shader
                               {
                                   [Push] protected WithPush Push;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       var grown = Push.Bounds with { Upper = Push.NewUpper };
                                       Push.Output[0] = grown;
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);

        Snapshot.Verify(result.Shaders["WithExpressionShader"]);
    }

    // Two `with`s with the same (type, overridden-field-set) shape share one synthesized helper.
    [Test]
    public void RepeatedSameShapeWithExpressionsShareOneHelper()
    {
        const string source = """
                               using System.Numerics;
                               using Rin.Shade;

                               namespace WithDedupeCheck;

                               [ShaderStruct]
                               public struct Bounds3D
                               {
                                   public Vector3 Lower;
                                   public Vector3 Upper;
                               }

                               public struct WithPush
                               {
                                   public Bounds3D Bounds;
                                   public Vector3 NewUpper;
                                   public BufferRef<Bounds3D> Output;
                               }

                               [Shader("Fixtures/with_dedupe.slang")]
                               public class WithDedupeShader : Shader
                               {
                                   [Push] protected WithPush Push;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       var a = Push.Bounds with { Upper = Push.NewUpper };
                                       var b = a with { Upper = Push.Bounds.Lower };
                                       Push.Output[0] = b;
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);

        var slang = result.Shaders["WithDedupeShader"];
        Assert.That(slang.Split("bounds3DWithUpper(Bounds3D self", System.StringSplitOptions.None).Length - 1,
            Is.EqualTo(1));
    }
}
