using Rin.Shade.Transpiler;

namespace Rin.Shade.Tests.Emitter;

// Vector2/3/4 and Matrix4x4 are BCL types with no source and no [SlangCall] binding - their
// constructors need their own intrinsic-passthrough carve-out (TypeMapping.IsIntrinsicVectorOrMatrixConstructor).
public class VectorConstructorTests
{
    [Test]
    public void VectorConstructorCallLowersDirectly()
    {
        const string source = """
                               using System.Numerics;
                               using Rin.Shade;

                               namespace VectorConstructorCheck;

                               public struct VectorConstructorPush
                               {
                                   public BufferRef<Vector3> Output;
                               }

                               [Shader("Fixtures/vector_constructor.slang")]
                               public class VectorConstructorShader : Shader
                               {
                                   [Push] protected VectorConstructorPush Push;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       var v = new Vector3(1f, 2f, 3f);
                                       Push.Output[0] = v;
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);

        const string expected = """
                                 struct VectorConstructorPush
                                 {
                                     float3* output;
                                 }

                                 [[vk::push_constant]] uniform ConstantBuffer<VectorConstructorPush, ScalarDataLayout> push;

                                 [shader("compute")]
                                 [numthreads(1, 1, 1)]
                                 void compute()
                                 {
                                     var v = float3(1, 2, 3);
                                     push.output[0] = v;
                                 }

                                 """;

        Assert.That(result.Shaders["VectorConstructorShader"].Replace("\r\n", "\n"),
            Is.EqualTo(expected.Replace("\r\n", "\n")));
    }
}
