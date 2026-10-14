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

                               [Shader]
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

                               [Shader]
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

    // A positional record struct's members are auto properties, which lower to fields, so `with` writes them.
    [Test]
    public void WithOnARecordStructWritesTheAutoPropertyField()
    {
        const string source = """
                               using System.Numerics;
                               using Rin.Shade;

                               namespace WithRecordCheck;

                               [ShaderStruct]
                               public readonly record struct Bounds3D(Vector3 Lower, Vector3 Upper);

                               public struct WithPush
                               {
                                   public Bounds3D Bounds;
                                   public Vector3 NewUpper;
                                   public BufferRef<Bounds3D> Output;
                               }

                               [Shader]
                               public class WithRecordShader : Shader
                               {
                                   [Push] protected WithPush Push;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       Push.Output[0] = Push.Bounds with { Upper = Push.NewUpper };
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);
        var slang = result.Shaders["WithRecordShader"].Replace("\r\n", "\n");
        Assert.That(slang, Does.Contain("Bounds3D bounds3DWithUpper(Bounds3D self, float3 upper)\n{\n    self.upper = upper;"));
        Assert.That(slang, Does.Contain("bounds3DWithUpper(push.bounds, push.newUpper)"));
    }

    [Test]
    public void WithOnAPropertyWithASetterBodyCallsTheSetter()
    {
        const string source = """
                               using Rin.Shade;

                               namespace WithSetterCheck;

                               [ShaderStruct]
                               public struct Sized
                               {
                                   public float Width;
                                   public float Height;

                                   public float Area
                                   {
                                       get => Width * Height;
                                       set
                                       {
                                           Width = value;
                                           Height = 1f;
                                       }
                                   }
                               }

                               public struct WithPush
                               {
                                   public Sized Sized;
                                   public float NewArea;
                                   public BufferRef<Sized> Output;
                               }

                               [Shader]
                               public class WithSetterShader : Shader
                               {
                                   [Push] protected WithPush Push;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       Push.Output[0] = Push.Sized with { Area = Push.NewArea };
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);
        var slang = result.Shaders["WithSetterShader"].Replace("\r\n", "\n");
        Assert.That(slang, Does.Contain("[mutating]"));
        Assert.That(slang, Does.Contain("Sized sizedWithArea(Sized self, float area)\n{\n    self.setArea(area);"));
    }
}
