using System.Linq;

namespace Rin.Shade.Tests.Emitter;

using Rin.Shade.Transpiler;

/// <summary>
/// Five more silent-wrong-output gaps found by re-reading the emitter against real C# 14 extension
/// methods, generics, non-default enum underlying types, multi-dimensional/jagged arrays, and
/// pattern-matching switch clauses - none of these previously produced a diagnostic, they'd have
/// silently emitted broken or colliding Slang instead.
/// </summary>
public class MoreDiagnosticsTests
{
    [Test]
    public void ExtensionMethodIsRejected()
    {
        const string source = """
                               using System.Numerics;
                               using Rin.Shade;

                               namespace ExtensionMethodCheck;

                               public static class VectorExtras
                               {
                                   extension(Vector4 v)
                                   {
                                       public float Foo() => v.X + v.Y;
                                   }
                               }

                               [Shader("Fixtures/ext_method.slang")]
                               public class ExtMethodShader : Shader
                               {
                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       var v = new Vector4(1f, 2f, 3f, 4f);
                                       var r = v.Foo();
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics.Any(d => d.Id == "SHADE0007"), Is.True);
    }

    [Test]
    public void GenericStructIsRejected()
    {
        const string source = """
                               using Rin.Shade;

                               namespace GenericStructCheck;

                               public struct Wrapper<T>
                               {
                                   public float Value;
                               }

                               public struct GenericPush
                               {
                                   public Wrapper<int> W;
                                   public BufferRef<float> Output;
                               }

                               [Shader("Fixtures/generic_struct.slang")]
                               public class GenericStructShader : Shader
                               {
                                   [Push] protected GenericPush Push;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       Push.Output[0] = 1f;
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics.Any(d => d.Id == "SHADE0008"), Is.True);
    }

    [Test]
    public void NonDefaultEnumUnderlyingTypeIsRejected()
    {
        const string source = """
                               using Rin.Shade;

                               namespace EnumUnderlyingTypeCheck;

                               public enum SmallMode : byte
                               {
                                   Opaque = 0,
                                   Translucent = 1
                               }

                               public struct EnumPush
                               {
                                   public SmallMode Mode;
                                   public BufferRef<float> Output;
                               }

                               [Shader("Fixtures/enum_underlying.slang")]
                               public class EnumUnderlyingShader : Shader
                               {
                                   [Push] protected EnumPush Push;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       Push.Output[0] = 1f;
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics.Any(d => d.Id == "SHADE0009"), Is.True);
    }

    [Test]
    public void MultiDimensionalArrayIsRejected()
    {
        const string source = """
                               using Rin.Shade;

                               namespace MultiDimArrayCheck;

                               public struct GridPush
                               {
                                   [FixedSize(4)] public float[,] Grid;
                                   public BufferRef<float> Output;
                               }

                               [Shader("Fixtures/multi_dim.slang")]
                               public class MultiDimShader : Shader
                               {
                                   [Push] protected GridPush Push;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       Push.Output[0] = 1f;
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics.Any(d => d.Id == "SHADE0010"), Is.True);
    }

    [Test]
    public void PatternMatchingSwitchClauseIsRejected()
    {
        const string source = """
                               using Rin.Shade;

                               namespace SwitchPatternCheck;

                               [Shader("Fixtures/switch_pattern.slang")]
                               public class SwitchPatternShader : Shader
                               {
                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       var x = 5;
                                       switch (x)
                                       {
                                           case > 3:
                                               x = 1;
                                               break;
                                           default:
                                               x = 0;
                                               break;
                                       }
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics.Any(d => d.Id == "SHADE0011"), Is.True);
    }
}
