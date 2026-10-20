using System.Linq;
using Rin.Shade.Transpiler;

namespace Rin.Shade.Tests.Emitter;

public class SwitchExpressionTests
{
    [Test]
    public void SwitchExpressionLowersToATernaryChain()
    {
        const string source = """
                               using System.Numerics;
                               using Rin.Shade;

                               namespace SwitchExpressionCheck;

                               public enum Shape
                               {
                                   Circle,
                                   Square,
                                   Diamond
                               }

                               public struct SwitchPush
                               {
                                   public Shape Shape;
                                   public float Distance;
                                   public int Lane;
                                   public Vector4 Value;
                                   public BufferRef<float> Output;
                               }

                               [Shader]
                               public class SwitchExpressionShader : Shader
                               {
                                   [Push] protected SwitchPush Push;

                                   private static float Lane(Vector4 value, int index) => index switch
                                   {
                                       0 => value.X,
                                       1 => value.Y,
                                       2 => value.Z,
                                       _ => value.W
                                   };

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       var radius = Push.Shape switch
                                       {
                                           Shape.Circle => 1f,
                                           Shape.Square => 1.5f,
                                           _ => 2f
                                       };
                                       var band = (Push.Distance * 2f) switch
                                       {
                                           < 0.25f => 0f,
                                           >= 0.25f and < 0.75f => 0.5f,
                                           > 0.9f or < -0.9f when Push.Lane > 0 => 0.75f,
                                           _ => 1f
                                       };
                                       Push.Output[0] = radius + band + Lane(Push.Value, Push.Lane);
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);

        Snapshot.Verify(result.Shaders["SwitchExpressionShader"]);
    }

    [Test]
    public void ASwitchExpressionInALoopHeaderIsNotHoisted()
    {
        const string source = """
                               using Rin.Shade;

                               namespace SwitchLoopCheck;

                               public struct LoopPush
                               {
                                   public int Mode;
                                   public BufferRef<float> Output;
                               }

                               [Shader]
                               public class SwitchLoopShader : Shader
                               {
                                   [Push] protected LoopPush Push;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       var sum = 0f;
                                       for (var i = 0; i < (Push.Mode + 1) switch { 1 => 4, _ => 8 }; i++) sum += i;
                                       Push.Output[0] = sum;
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);
        var slang = result.Shaders["SwitchLoopShader"];
        Assert.Multiple(() =>
        {
            Assert.That(slang, Does.Not.Contain("switchValue"));
            Assert.That(slang, Does.Contain("i < (push.mode + 1 == 1 ? 4 : 8)"));
        });
    }

    [Test]
    public void ATypePatternArmIsRejected()
    {
        const string source = """
                               using Rin.Shade;

                               namespace SwitchTypePatternCheck;

                               public struct PatternPush
                               {
                                   public int Mode;
                                   public BufferRef<float> Output;
                               }

                               [Shader]
                               public class TypePatternShader : Shader
                               {
                                   [Push] protected PatternPush Push;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       object boxed = Push.Mode;
                                       Push.Output[0] = boxed switch { int n => n, _ => 0f };
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics.Any(d => d.Id == Diagnostics.Emitter.UnsupportedSwitchClause.Id), Is.True);
    }
}
