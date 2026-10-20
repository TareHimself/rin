using Rin.Shade.Transpiler;

namespace Rin.Shade.Tests.Emitter;

public class GenericStructTests
{
    [Test]
    public void ConcreteGenericStructsLowerOncePerInstantiation()
    {
        const string source = """
                               using System.Numerics;
                               using Rin.Shade;

                               namespace GenericStructCheck;

                               public struct Pair<T> where T : unmanaged
                               {
                                   public T First;
                                   public T Second;

                                   public T Pick(bool first) => first ? First : Second;

                                   public static Pair<T> Of(T first, T second)
                                   {
                                       Pair<T> pair;
                                       pair.First = first;
                                       pair.Second = second;
                                       return pair;
                                   }
                               }

                               public struct GenericPush
                               {
                                   public Pair<float> Floats;
                                   public Pair<Vector3> Vectors;
                                   public int Flag;
                                   public BufferRef<Vector3> Output;
                               }

                               [Shader]
                               public class GenericStructShader : Shader
                               {
                                   [Push] protected GenericPush Push;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       var first = Push.Flag != 0;
                                       var scale = Pair<float>.Of(Push.Floats.Pick(first), 2f);
                                       Push.Output[0] = Push.Vectors.Pick(first) * scale.Pick(true);
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);

        Snapshot.Verify(result.Shaders["GenericStructShader"]);
    }

    [Test]
    public void GenericStructUsedInsideGenericMethodIsSpelledWithTheInstantiation()
    {
        const string source = """
                               using System.Numerics;
                               using Rin.Shade;

                               namespace GenericStructInMethodCheck;

                               public struct Box<T> where T : unmanaged
                               {
                                   public T Value;
                               }

                               public static class Helpers
                               {
                                   public static T Unbox<T>(T value) where T : unmanaged
                                   {
                                       Box<T> box;
                                       box.Value = value;
                                       return box.Value;
                                   }
                               }

                               [Shader]
                               public class GenericStructInMethodShader : Shader
                               {
                                   public struct BoxPush
                                   {
                                       public float A;
                                       public Vector2 B;
                                       public BufferRef<Vector2> Output;
                                   }

                                   [Push] protected BoxPush Push;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       Push.Output[0] = Helpers.Unbox(Push.B) * Helpers.Unbox(Push.A);
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);
        var slang = result.Shaders["GenericStructInMethodShader"];
        Assert.Multiple(() =>
        {
            Assert.That(slang, Does.Contain("struct Box_float"));
            Assert.That(slang, Does.Contain("struct Box_float2"));
            Assert.That(slang, Does.Contain("Box_float box;"));
            Assert.That(slang, Does.Contain("Box_float2 box;"));
            Assert.That(slang, Does.Not.Contain("Box<"));
        });
    }
}
