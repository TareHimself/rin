using System.Linq;
using Rin.Shade.Transpiler;

namespace Rin.Shade.Tests.Emitter;

public class ShaderUnionTests
{
    private const string Types = """
        using System.Numerics;
        using System.Runtime.InteropServices;
        using Rin.Shade;

        namespace UnionCheck;

        public struct LineData
        {
            public Vector2 Begin;
            public float Thickness;
        }

        public struct CircleData
        {
            public Matrix4x4 Inverse;
            public float Radius;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct Extra
        {
            [FieldOffset(0)] public float Tag;
            [FieldOffset(4)] public Vector2 A;
            [FieldOffset(4)] public Vector4 B;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct Quad
        {
            [FieldOffset(0)] public Vector4 Opts;
            [FieldOffset(16)] public Vector2 Size;
            [FieldOffset(24)] public LineData Line;
            [FieldOffset(24)] public CircleData Circle;
            [FieldOffset(24)] public Extra Extra;
        }
        """;

    private static string Shader(string body, string types = Types) => types + $$"""


        public struct UnionPush
        {
            public Quad Quad;
            public BufferRef<float> Output;
        }

        [Shader("Fixtures/union.slang")]
        public class UnionShader : Shader
        {
            [Push] protected UnionPush Push;

            [Compute(1, 1, 1)]
            public void Compute()
            {
                {{body}}
            }
        }
        """;

    [Test]
    public void UnionLowersToHeaderVariantsAndCompleteStruct()
    {
        var result = ShadeEmitter.Emit(CompilationBuilder.Build(Shader("Push.Output[0] = Push.Quad.Size.X;")));

        Assert.That(result.Diagnostics, Is.Empty);
        var slang = result.Shaders["UnionShader"].Replace("\r\n", "\n");

        Assert.That(slang, Does.Contain("    struct Quad\n    {\n        float4 opts;\n        float2 size;\n    }"));
        Assert.That(slang, Does.Contain("    struct __Shade__Quad_Line\n    {\n        Quad header;\n        LineData payload;\n    }"));
        Assert.That(slang, Does.Contain("    struct __Shade__Quad_Circle\n    {\n        Quad header;\n        CircleData payload;\n    }"));
        Assert.That(slang, Does.Contain("    struct __Shade__Quad_Extra\n    {\n        Quad header;\n        Extra payload;\n    }"));
        Assert.That(slang, Does.Contain("    struct __Shade__Quad_Extra_A\n    {\n        Quad header;\n        __Shade__Extra_A payload;\n    }"));
        Assert.That(slang, Does.Contain("    struct __Shade__Extra_A\n    {\n        Extra header;\n        float2 payload;\n    }"));
        Assert.That(slang, Does.Contain("    struct __Shade__CompleteQuad\n    {\n        Quad header;\n        uint _padding[17];\n    }"));
    }

    [Test]
    public void UnionValuesUseTheCompleteStructAndHeaderFieldsGoThroughHeader()
    {
        var result = ShadeEmitter.Emit(CompilationBuilder.Build(Shader("Push.Output[0] = Push.Quad.Size.X + Push.Quad.Extra.Tag;")));

        Assert.That(result.Diagnostics, Is.Empty);
        var slang = result.Shaders["UnionShader"];

        Assert.That(slang, Does.Contain("__Shade__CompleteQuad quad;"));
        Assert.That(slang, Does.Contain("push.quad.header.size.x"));
        Assert.That(slang, Does.Contain("reinterpret<__Shade__Quad_Extra>(push.quad).payload.tag"));
    }

    [Test]
    public void VariantAccessReinterpretsTheRootInstance()
    {
        var result = ShadeEmitter.Emit(CompilationBuilder.Build(Shader(
            "Push.Output[0] = Push.Quad.Line.Thickness + Push.Quad.Extra.B.X;")));

        Assert.That(result.Diagnostics, Is.Empty);
        var slang = result.Shaders["UnionShader"];

        Assert.That(slang, Does.Contain("reinterpret<__Shade__Quad_Line>(push.quad).payload.thickness"));
        Assert.That(slang, Does.Contain("reinterpret<__Shade__Quad_Extra_B>(push.quad).payload.payload.x"));
    }

    private static string BufferShader(string body) => Shader(body).Replace(
        "public Quad Quad;", "public BufferRef<Quad> Quads;");

    [Test]
    public void WritingThroughABufferElementVariantIsInPlace()
    {
        var result = ShadeEmitter.Emit(CompilationBuilder.Build(BufferShader("""
            Push.Quads[0].Line.Thickness = 1f;
            ref var quad = ref Push.Quads[1];
            quad.Circle.Radius = 2f;
            Push.Output[0] = Push.Quads[2].Extra.B.X;
            """)));

        Assert.That(result.Diagnostics, Is.Empty);
        var slang = result.Shaders["UnionShader"];

        Assert.That(slang, Does.Contain("((__Shade__Quad_Line*)(push.quads + 0))->payload.thickness = 1;"));
        Assert.That(slang, Does.Contain("var quad = push.quads + 1;"));
        Assert.That(slang, Does.Contain("((__Shade__Quad_Circle*)(quad))->payload.radius = 2;"));
        Assert.That(slang, Does.Contain("((__Shade__Quad_Extra_B*)(push.quads + 2))->payload.payload.x"));
    }

    [Test]
    public void CopyingAnElementThenReadingUsesReinterpret()
    {
        var result = ShadeEmitter.Emit(CompilationBuilder.Build(BufferShader("""
            var copy = Push.Quads[0];
            Push.Output[0] = copy.Line.Thickness;
            """)));

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Shaders["UnionShader"], Does.Contain("reinterpret<__Shade__Quad_Line>(copy).payload.thickness"));
    }

    [Test]
    public void RefLocalOfANonBufferElementIsRejected()
    {
        var result = ShadeEmitter.Emit(CompilationBuilder.Build(Shader("""
            var local = 1f;
            ref var alias = ref local;
            Push.Output[0] = alias;
            """)));

        Assert.That(result.Diagnostics.Any(d => d.Id == Diagnostics.Emitter.UnsupportedConstruct.Id), Is.True);
    }

    [Test]
    public void AssigningThroughAVariantIsRejected()
    {
        var result = ShadeEmitter.Emit(CompilationBuilder.Build(Shader("Push.Quad.Line.Thickness = 1f;")));

        Assert.That(result.Diagnostics.Any(d => d.Id == Diagnostics.Emitter.UnionVariantWrite.Id), Is.True);
    }

    [Test]
    public void UnionWithNoOverlapIsRejected()
    {
        const string types = """
            using System.Numerics;
            using System.Runtime.InteropServices;
            using Rin.Shade;

            namespace UnionCheck;

                [StructLayout(LayoutKind.Explicit)]
            public struct Quad
            {
                [FieldOffset(0)] public Vector4 Opts;
                [FieldOffset(16)] public Vector2 Size;
            }
            """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(Shader("Push.Output[0] = Push.Quad.Size.X;", types)));

        Assert.That(result.Diagnostics.Any(d => d.Id == Diagnostics.Emitter.InvalidUnionLayout.Id), Is.True);
    }

    [Test]
    public void HeaderFieldAfterTheUnionRegionIsRejected()
    {
        const string types = """
            using System.Numerics;
            using System.Runtime.InteropServices;
            using Rin.Shade;

            namespace UnionCheck;

                [StructLayout(LayoutKind.Explicit)]
            public struct Quad
            {
                [FieldOffset(0)] public Vector2 A;
                [FieldOffset(0)] public Vector4 B;
                [FieldOffset(16)] public float Tail;
            }
            """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(Shader("Push.Output[0] = Push.Quad.Tail;", types)));

        Assert.That(result.Diagnostics.Any(d => d.Id == Diagnostics.Emitter.InvalidUnionLayout.Id), Is.True);
    }
}
