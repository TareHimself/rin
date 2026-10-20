using Rin.Shade.Transpiler;

namespace Rin.Shade.Tests.Emitter;

public class GenericMethodTests
{
    [Test]
    public void GenericMethodsLowerOncePerInstantiation()
    {
        const string source = """
                               using System.Numerics;
                               using Rin.Shade;

                               namespace GenericMethodCheck;

                               public static class Helpers
                               {
                                   public static T Pick<T>(bool first, T a, T b) where T : unmanaged
                                   {
                                       T chosen = first ? a : b;
                                       return chosen;
                                   }

                                   public static T Twice<T>(bool first, T a, T b) where T : unmanaged =>
                                       Pick(first, Pick(first, a, b), b);

                                   public static T Load<T>(BufferRef<T> buffer, int index) where T : unmanaged => buffer[index];
                               }

                               public struct GenericPush
                               {
                                   public int Flag;
                                   public float A;
                                   public Vector3 B;
                                   public BufferRef<float> Floats;
                                   public BufferRef<Vector3> Output;
                               }

                               [Shader]
                               public class GenericMethodShader : Shader
                               {
                                   [Push] protected GenericPush Push;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       var first = Push.Flag != 0;
                                       var f = Helpers.Twice(first, Push.A, Helpers.Load(Push.Floats, 1));
                                       Push.Output[0] = Helpers.Pick(first, Push.B, new Vector3(f));
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);

        Snapshot.Verify(result.Shaders["GenericMethodShader"]);
    }

    [Test]
    public void InstantiationsThatDifferOnlyInTypeArgumentsGetDistinctNames()
    {
        const string source = """
                               using System.Numerics;
                               using Rin.Shade;

                               namespace GenericNameCheck;

                               public static class Helpers
                               {
                                   public static int SizeTag<T>() where T : unmanaged => 1;
                               }

                               [Shader]
                               public class GenericNameShader : Shader
                               {
                                   public struct NamePush
                                   {
                                       public BufferRef<int> Output;
                                   }

                                   [Push] protected NamePush Push;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       Push.Output[0] = Helpers.SizeTag<float>() + Helpers.SizeTag<Vector2>() + Helpers.SizeTag<BufferRef<Vector4>>();
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);
        var slang = result.Shaders["GenericNameShader"];
        Assert.Multiple(() =>
        {
            Assert.That(slang, Does.Contain("int sizeTag_float()"));
            Assert.That(slang, Does.Contain("int sizeTag_float2()"));
            Assert.That(slang, Does.Contain("int sizeTag_float4Ptr()"));
            Assert.That(slang, Does.Contain("sizeTag_float() + sizeTag_float2() + sizeTag_float4Ptr()"));
        });
    }
}
