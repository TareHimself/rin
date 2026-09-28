namespace Rin.Shade.Tests.Emitter;

using Rin.Shade.Transpiler;

/// <summary>
/// Covers the silent-wrong-output bugs found while auditing BodyLowering against the doc's C#
/// subset list: static/enum field references, dropped ref/out parameter modifiers, fixed-size
/// arrays, and matrix mul() lowering. Struct instance methods ("methods on structs" in the subset)
/// are explicitly NOT covered here - real Slang lets a struct declare its own methods with an
/// implicit `this`, and correctly emitting that needs StructLowering to discover and nest methods
/// inside the struct body, which isn't implemented yet. The call-site fix (reading
/// invocation.Instance) is real and stays, but "methods on structs" end to end is a follow-up.
/// </summary>
public class CorrectnessTests
{
    [Test]
    public void EmitsEnumsMatricesFixedArraysRefParamsAndMatrixMul()
    {
        var source = FixtureSource.Read("../Fixtures/CorrectnessFixtureShader.cs");
        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);
        var slang = result.Shaders["CorrectnessFixtureShader"];

        // Enum lowering, with explicit values.
        Assert.That(slang, Does.Contain("enum BlendMode"));
        Assert.That(slang, Does.Contain("Opaque = 0,"));
        Assert.That(slang, Does.Contain("Translucent = 1"));

        // Static/enum member reference - previously threw a NullReferenceException.
        Assert.That(slang, Does.Contain("push.mode == BlendMode.Opaque"));

        // Matrix4x4 -> float4x4, and matrix*matrix -> mul(b, a) (the doc's transpose-avoidance rule).
        Assert.That(slang, Does.Contain("float4x4 view;"));
        Assert.That(slang, Does.Contain("mul(push.projection, push.view)"));

        // Fixed-size array field.
        Assert.That(slang, Does.Contain("float4 planes[6];"));

        // out parameters, both in the signature and preserved (not silently dropped) at the call site.
        Assert.That(slang, Does.Contain("void split(float value, out float whole, out float frac)"));
        Assert.That(slang, Does.Contain("split(1.5, whole, frac);"));

        // Uninitialized locals need their real type, not 'var' (nothing to infer from).
        Assert.That(slang, Does.Contain("float whole;"));
        Assert.That(slang, Does.Contain("float frac;"));
    }
}
