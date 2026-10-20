using System.Linq;
using Rin.Shade.Transpiler;

namespace Rin.Shade.Tests.Emitter;

public class ExtensionMemberTests
{
    [Test]
    public void ExtensionBlockMembersLowerToASlangExtension()
    {
        const string source = """
                               using System.Numerics;
                               using Rin.Shade;

                               namespace ExtensionBlockCheck;

                               public static class VectorExtras
                               {
                                   extension(in Vector4 v)
                                   {
                                       public float Sum() => v.X + v.Y + v.Z + v.W;

                                       public Vector4 Scaled(float by) => v * by;

                                       public float MaxComponent => Shader.Max(Shader.Max(v.X, v.Y), Shader.Max(v.Z, v.W));
                                   }

                                   extension(Vector3)
                                   {
                                       public static Vector3 Up() => new Vector3(0f, 1f, 0f);
                                   }
                               }

                               public struct ExtensionPush
                               {
                                   public Vector4 Value;
                                   public BufferRef<float> Output;
                               }

                               [Shader]
                               public class ExtensionBlockShader : Shader
                               {
                                   [Push] protected ExtensionPush Push;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       var v = Push.Value;
                                       Push.Output[0] = v.Scaled(2f).Sum() + Push.Value.MaxComponent + Vector3.Up().Y;
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);

        Snapshot.Verify(result.Shaders["ExtensionBlockShader"]);
    }

    [Test]
    public void ClassicExtensionMethodsLowerWithTheReceiverAsThis()
    {
        const string source = """
                               using System.Numerics;
                               using Rin.Shade;

                               namespace ClassicExtensionCheck;

                               public struct Bounds
                               {
                                   public Vector3 Lower;
                                   public Vector3 Upper;
                               }

                               public static class BoundsExtensions
                               {
                                   public static Vector3 Size(this Bounds b) => b.Upper - b.Lower;

                                   public static Bounds Grown(this Bounds b, float by)
                                   {
                                       b.Lower -= new Vector3(by);
                                       b.Upper += new Vector3(by);
                                       return b;
                                   }

                                   public static void Grow(ref this Bounds b, float by)
                                   {
                                       b.Lower -= new Vector3(by);
                                       b.Upper += new Vector3(by);
                                   }
                               }

                               public struct BoundsPush
                               {
                                   public Bounds Bounds;
                                   public BufferRef<Vector3> Output;
                               }

                               [Shader]
                               public class ClassicExtensionShader : Shader
                               {
                                   [Push] protected BoundsPush Push;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       var bounds = Push.Bounds;
                                       bounds.Grow(1f);
                                       var grown = BoundsExtensions.Grown(bounds, 2f);
                                       Push.Output[0] = grown.Size() + bounds.Size();
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);

        Snapshot.Verify(result.Shaders["ClassicExtensionShader"]);
    }

    [Test]
    public void AnExtensionOfAnArrayIsRejected()
    {
        const string source = """
                               using Rin.Shade;

                               namespace ArrayExtensionCheck;

                               public static class ArrayExtras
                               {
                                   extension(float[] values)
                                   {
                                       public float First() => values[0];
                                   }
                               }

                               [Shader]
                               public class ArrayExtensionShader : Shader
                               {
                                   public struct ArrayPush
                                   {
                                       public BufferRef<float> Output;
                                   }

                                   [Push] protected ArrayPush Push;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       var values = new float[] { 1f, 2f };
                                       Push.Output[0] = values.First();
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics.Any(d => d.Id == Diagnostics.Emitter.ExtensionMethodNotSupported.Id), Is.True);
    }
}
