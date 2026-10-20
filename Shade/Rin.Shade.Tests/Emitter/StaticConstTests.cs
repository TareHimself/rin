using System.Linq;
using Rin.Shade.Transpiler;

namespace Rin.Shade.Tests.Emitter;

public class StaticConstTests
{
    [Test]
    public void StaticReadonlyFieldsLowerToStaticConsts()
    {
        const string source = """
                               using System.Numerics;
                               using Rin.Shade;

                               namespace StaticConstCheck;

                               public static class Lighting
                               {
                                   public static readonly Vector3 Up = new Vector3(0f, 1f, 0f);
                                   public static readonly Vector3 LightDirection = new Vector3(-0.45f, 0.6f, 0.65f);
                                   public static readonly float Ambient = 0.12f;
                                   public static readonly float Diffuse = 1f - Ambient;
                                   public static readonly float[] Weights = { 0.25f, 0.5f, 0.25f };
                               }

                               public struct Kernel
                               {
                                   public static readonly int Taps = 3;
                                   public static readonly Vector2 Step = new Vector2(1f, 0f) * 2f;
                                   public float Scale;
                               }

                               public struct ConstPush
                               {
                                   public Kernel Kernel;
                                   public BufferRef<Vector3> Output;
                               }

                               [Shader]
                               public class StaticConstShader : Shader
                               {
                                   private static readonly Vector3 Tint = new Vector3(0.9f, 0.45f, 0.2f);

                                   [Push] protected ConstPush Push;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       var sum = 0f;
                                       for (var i = 0; i < Lighting.Weights.Length; i++) sum += Lighting.Weights[i];
                                       var shade = Lighting.Ambient + Lighting.Diffuse * Max(Dot(Lighting.Up, Lighting.LightDirection), 0f);
                                       Push.Output[0] = Tint * shade * sum * Kernel.Taps * Push.Kernel.Scale + new Vector3(Kernel.Step, 0f);
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);

        Snapshot.Verify(result.Shaders["StaticConstShader"]);
    }

    [Test]
    public void OnlyTheConstantsAShaderReadsAreDeclared()
    {
        const string source = """
                               using System.Numerics;
                               using Rin.Shade;

                               namespace StaticConstPruneCheck;

                               public static class Table
                               {
                                   public static readonly float Used = 2f;
                                   public static readonly float Unused = 3f;
                               }

                               [Shader]
                               public class PruneShader : Shader
                               {
                                   public struct PrunePush
                                   {
                                       public BufferRef<float> Output;
                                   }

                                   [Push] protected PrunePush Push;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       Push.Output[0] = Table.Used;
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);
        var slang = result.Shaders["PruneShader"];
        Assert.Multiple(() =>
        {
            Assert.That(slang, Does.Contain("static const float used = 2;"));
            Assert.That(slang, Does.Not.Contain("unused"));
            Assert.That(slang, Does.Contain("push.output[0] = Table::used;"));
        });
    }

    [Test]
    public void AStaticReadonlyWithACallInItsInitializerIsRejected()
    {
        const string source = """
                               using System.Numerics;
                               using Rin.Shade;

                               namespace StaticConstCallCheck;

                               public static class Table
                               {
                                   public static readonly Vector3 Forward = Shader.Normalize(new Vector3(1f, 1f, 0f));
                               }

                               [Shader]
                               public class CallInitializerShader : Shader
                               {
                                   public struct CallPush
                                   {
                                       public BufferRef<Vector3> Output;
                                   }

                                   [Push] protected CallPush Push;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       Push.Output[0] = Table.Forward;
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics.Select(d => d.GetMessage()),
            Has.Some.Contains("Forward").And.Some.Contains("not a constant expression"));
    }
}
