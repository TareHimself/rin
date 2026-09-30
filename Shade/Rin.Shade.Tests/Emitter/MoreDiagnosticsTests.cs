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

        Assert.That(result.Diagnostics.Any(d => d.Id == Diagnostics.Emitter.ExtensionMethodNotSupported.Id), Is.True);
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

        Assert.That(result.Diagnostics.Any(d => d.Id == Diagnostics.Emitter.GenericNotSupported.Id), Is.True);
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

        Assert.That(result.Diagnostics.Any(d => d.Id == Diagnostics.Emitter.UnsupportedEnumUnderlyingType.Id), Is.True);
    }

    [Test]
    public void PlainArrayParameterIsRejected()
    {
        const string source = """
                               using Rin.Shade;

                               namespace ArrayParameterCheck;

                               public struct ArrayParameterPush
                               {
                                   public BufferRef<float> Output;
                               }

                               [Shader("Fixtures/array_parameter.slang")]
                               public class ArrayParameterShader : Shader
                               {
                                   [Push] protected ArrayParameterPush Push;

                                   private float First(float[] values) => 0f;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       Push.Output[0] = First(null);
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics.Any(d => d.Id == Diagnostics.Emitter.UnsupportedType.Id), Is.True);
    }

    [Test]
    public void PlainArrayFieldIsRejected()
    {
        const string source = """
                               using Rin.Shade;

                               namespace PlainArrayCheck;

                               public struct GridPush
                               {
                                   public float[] Grid;
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

        Assert.That(result.Diagnostics.Any(d => d.Id == Diagnostics.Emitter.UnsupportedType.Id), Is.True);
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

        Assert.That(result.Diagnostics.Any(d => d.Id == Diagnostics.Emitter.UnsupportedSwitchClause.Id), Is.True);
    }

    // Found while porting a real shader (bounds_update.slang): a field named "Min" lowers to the
    // Slang identifier "min" - when one of the struct's own instance methods then calls the builtin
    // min(...), the unqualified call resolves to the field instead (confirmed against the real Slang
    // compiler), and fails to compile. A struct merely having a field named "min" is fine on its own -
    // the collision only exists once something inside the struct's own scope actually calls it.
    [Test]
    public void FieldNameCollidingWithCalledFunctionIsRejected()
    {
        const string source = """
                               using System.Numerics;
                               using Rin.Shade;

                               namespace BuiltinShadowCheck;

                               public static class VectorIntrinsics
                               {
                                   [SlangExpression("min(@0, @1)")] public static extern Vector3 Min(Vector3 a, Vector3 b);
                               }

                               [ShaderStruct]
                               public struct ShadowingBounds
                               {
                                   public Vector3 Min;

                                   public void Grow(Vector3 v)
                                   {
                                       Min = VectorIntrinsics.Min(Min, v);
                                   }
                               }

                               public struct BuiltinShadowPush
                               {
                                   public ShadowingBounds Bounds;
                                   public BufferRef<float> Output;
                               }

                               [Shader("Fixtures/builtin_shadow.slang")]
                               public class BuiltinShadowShader : Shader
                               {
                                   [Push] protected BuiltinShadowPush Push;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       var bounds = Push.Bounds;
                                       bounds.Grow(new Vector3(1f, 1f, 1f));
                                       Push.Output[0] = 1f;
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics.Any(d => d.Id == Diagnostics.Emitter.FieldShadowsCalledFunction.Id), Is.True);
    }
}
