using System.Linq;

namespace Rin.Shade.Tests.Emitter;

using Rin.Shade.Transpiler;

/// <summary>
/// One test per item in the doc's "Rejected in v1" list that's realistically reachable without a
/// C# compile error of its own (unsafe code needs AllowUnsafeBlocks at the project level, which
/// this test project doesn't set, so it's rejected by the ambient project configuration rather
/// than needing its own transpiler-level test here).
/// </summary>
public class RejectionCoverageTests
{
    [Test]
    public void StringTypedFieldIsRejected()
    {
        const string source = """
                               using Rin.Shade;

                               namespace StringFieldCheck;

                               public struct StringPush
                               {
                                   public string Name;
                                   public BufferRef<float> Output;
                               }

                               [Shader("Fixtures/string_field.slang")]
                               public class StringFieldShader : Shader
                               {
                                   [Push] protected StringPush Push;

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
    public void LambdaIsRejected()
    {
        const string source = """
                               using System;
                               using Rin.Shade;

                               namespace LambdaCheck;

                               [Shader("Fixtures/lambda.slang")]
                               public class LambdaShader : Shader
                               {
                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       Func<float, float> f = x => x * 2f;
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics.Any(d => d.Id == Diagnostics.Emitter.UnsupportedConstruct.Id), Is.True);
    }

    [Test]
    public void LinqIsRejected()
    {
        const string source = """
                               using System.Linq;
                               using Rin.Shade;

                               namespace LinqCheck;

                               [Shader("Fixtures/linq.slang")]
                               public class LinqShader : Shader
                               {
                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       var count = Enumerable.Range(0, 4).Count();
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        // Not pinned to one diagnostic ID: Enumerable.Range/.Count() have no [SlangCall] binding and
        // no source, so this can surface as either "unsupported construct" or "no source for
        // method" depending on which operation the walk reaches first - what matters is that LINQ
        // is never silently accepted, and emission finishes rather than crashing.
        Assert.That(result.Diagnostics, Is.Not.Empty);
    }

    [Test]
    public void RefReturnIsRejected()
    {
        const string source = """
                               using Rin.Shade;

                               namespace RefReturnCheck;

                               public static class RefReturnHelpers
                               {
                                   public static ref float Identity(ref float x) => ref x;
                               }

                               [Shader("Fixtures/ref_return.slang")]
                               public class RefReturnShader : Shader
                               {
                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       float value = 1f;
                                       var result = RefReturnHelpers.Identity(ref value);
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics.Any(d => d.Id == Diagnostics.Emitter.UnsupportedConstruct.Id), Is.True);
    }

    [Test]
    public void GenericShaderClassIsRejected()
    {
        const string source = """
                               using Rin.Shade;

                               namespace GenericShaderCheck;

                               [Shader("Fixtures/generic.slang")]
                               public class GenericShader<T> : Shader
                               {
                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics.Any(d => d.Id == Diagnostics.Emitter.UnsupportedConstruct.Id), Is.True);
    }

    [Test]
    public void UserDefinedOperatorOverloadIsRejected()
    {
        const string source = """
                               using Rin.Shade;

                               namespace OperatorOverloadCheck;

                               public struct Fixed8
                               {
                                   public int Raw;

                                   public static Fixed8 operator +(Fixed8 a, Fixed8 b) => new() { Raw = a.Raw + b.Raw };
                               }

                               [Shader("Fixtures/operator_overload.slang")]
                               public class OperatorOverloadShader : Shader
                               {
                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       var a = new Fixed8 { Raw = 1 };
                                       var b = new Fixed8 { Raw = 2 };
                                       var c = a + b;
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        // Not asserting on the diagnostic count: `new Fixed8 { Raw = ... }` (object-initializer
        // syntax, also unimplemented) fires its own diagnostic for each of `a`/`b` before the `+`
        // is even reached - checking the specific message proves the operator-overload check
        // itself fired, regardless of what else also got flagged along the way.
        Assert.That(result.Diagnostics.Any(d => d.GetMessage().Contains("user-defined operator overload")),
            Is.True);
    }
}
