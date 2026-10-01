using Rin.Shade.Transpiler;

namespace Rin.Shade.Tests.Emitter;

public class SemanticAttributeTests
{
    private const string Source = """
        using System;
        using System.Numerics;
        using Rin.Shade;

        namespace SemanticCheck;

        public sealed class TexCoordAttribute() : SemanticAttribute(SemanticName)
        {
            public const string SemanticName = "TEXCOORD";
        }

        public struct Push { public Matrix4x4 Projection; }

        public struct VertexIn
        {
            [VertexId] public int VertexId;
            [InstanceId] public int InstanceId;
        }

        public struct VertexOut
        {
            [Position] public Vector4 Position;
            [TexCoord] public Vector2 Uv;
            [Semantic("INDEX")] public int Index;
        }

        public struct FragmentIn
        {
            [TexCoord] public Vector2 Uv;
        }

        public struct FragmentOut
        {
            [Target(0)] public Vector4 Color;
            [Target(1)] public Vector4 Normal;
        }

        [Shader("Fixtures/semantics.slang")]
        public class SemanticShader : Shader
        {
            [Push] protected Push Push;

            [Vertex]
            public VertexOut Vertex(VertexIn input)
            {
                VertexOut output;
                output.Position = new Vector4(0f, 0f, 0f, 1f);
                output.Uv = new Vector2(0f, 0f);
                output.Index = input.InstanceId;
                return output;
            }

            [Fragment, Attachment(AttachmentFormat.RGBA16)]
            public FragmentOut Fragment(FragmentIn input)
            {
                FragmentOut output;
                output.Color = new Vector4(input.Uv, 0f, 1f);
                output.Normal = new Vector4(0f);
                return output;
            }
        }
        """;

    [Test]
    public void HelperAttributesLowerToTheirSemantics()
    {
        var result = ShadeEmitter.Emit(CompilationBuilder.Build(Source));

        Assert.That(result.Diagnostics, Is.Empty);
        var slang = result.Shaders["SemanticShader"].Replace("\r\n", "\n");
        Assert.That(slang, Does.Contain("int vertexId : SV_VertexID;"));
        Assert.That(slang, Does.Contain("int instanceId : SV_InstanceID;"));
        Assert.That(slang, Does.Contain("float4 position : SV_Position;"));
        Assert.That(slang, Does.Contain("float2 uv : TEXCOORD;"));
        Assert.That(slang, Does.Contain("int index : INDEX;"));
        Assert.That(slang, Does.Contain("float4 color : SV_Target0;"));
        Assert.That(slang, Does.Contain("float4 normal : SV_Target1;"));
    }
}
