using Rin.Shade.Transpiler;

namespace Rin.Shade.Tests.Emitter;

// Unary operators, compound assignment, ternary expressions, and Shader.Math (the shipped
// [SlangExpression] math library replacing per-fixture hand-rolled externs).
public class NewOperatorsAndIntrinsicsTests
{
    [Test]
    public void UnaryCompoundAndTernaryLowerCorrectly()
    {
        const string source = """
                               using System.Numerics;
                               using Rin.Shade;

                               namespace OperatorCheck;

                               public struct OperatorPush
                               {
                                   public Vector3 Normal;
                                   public int Count;
                                   public bool Flag;
                                   public BufferRef<float> Output;
                               }

                               [Shader("Fixtures/operators.slang")]
                               public class OperatorShader : Shader
                               {
                                   [Push] protected OperatorPush Push;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       var flipped = -Push.Normal;
                                       var notFlag = !Push.Flag;

                                       var total = 0;
                                       total += Push.Count;
                                       total -= 1;

                                       var chosen = Push.Flag ? 1f : 0f;

                                       Push.Output[0] = chosen;
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);

        Snapshot.Verify(result.Shaders["OperatorShader"]);
    }

    [Test]
    public void ShaderMathLowersToSlangBuiltins()
    {
        const string source = """
                               using System.Numerics;
                               using Rin.Shade;

                               namespace MathIntrinsicsCheck;

                               public struct MathPush
                               {
                                   public Vector3 A;
                                   public Vector3 B;
                                   public BufferRef<float> Output;
                               }

                               [Shader("Fixtures/math_intrinsics.slang")]
                               public class MathIntrinsicsShader : Shader
                               {
                                   [Push] protected MathPush Push;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       var n = Math.Normalize(Push.A);
                                       var d = Math.Dot(n, Push.B);
                                       var c = Math.Clamp(d, 0f, 1f);
                                       Push.Output[0] = c;
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);

        Snapshot.Verify(result.Shaders["MathIntrinsicsShader"]);
    }
}
