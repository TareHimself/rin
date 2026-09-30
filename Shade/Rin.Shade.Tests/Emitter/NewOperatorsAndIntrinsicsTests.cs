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

        const string expected = """
                                 namespace OperatorCheck
                                 {
                                     struct OperatorPush
                                     {
                                         float3 normal;
                                         int count;
                                         bool flag;
                                         float* output;
                                     }

                                 }

                                 [[vk::push_constant]] uniform ConstantBuffer<OperatorCheck::OperatorPush, ScalarDataLayout> push;

                                 [shader("compute")]
                                 [numthreads(1, 1, 1)]
                                 void compute()
                                 {
                                     var flipped = -push.normal;
                                     var notFlag = !push.flag;
                                     var total = 0;
                                     total += push.count;
                                     total -= 1;
                                     var chosen = push.flag ? 1 : 0;
                                     push.output[0] = chosen;
                                 }

                                 """;

        Assert.That(result.Shaders["OperatorShader"].Replace("\r\n", "\n"),
            Is.EqualTo(expected.Replace("\r\n", "\n")));
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

        const string expected = """
                                 namespace MathIntrinsicsCheck
                                 {
                                     struct MathPush
                                     {
                                         float3 a;
                                         float3 b;
                                         float* output;
                                     }

                                 }

                                 [[vk::push_constant]] uniform ConstantBuffer<MathIntrinsicsCheck::MathPush, ScalarDataLayout> push;

                                 [shader("compute")]
                                 [numthreads(1, 1, 1)]
                                 void compute()
                                 {
                                     var n = normalize(push.a);
                                     var d = dot(n, push.b);
                                     var c = clamp(d, 0, 1);
                                     push.output[0] = c;
                                 }

                                 """;

        Assert.That(result.Shaders["MathIntrinsicsShader"].Replace("\r\n", "\n"),
            Is.EqualTo(expected.Replace("\r\n", "\n")));
    }
}
