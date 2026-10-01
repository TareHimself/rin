using System.Linq;
using Rin.Shade.Transpiler;

namespace Rin.Shade.Tests.Emitter;

public class GraphicsShaderEmitterTests
{
    [Test]
    public void EmitsGraphicsTriangleFixtureShader()
    {
        var source = FixtureSource.Read("../Fixtures/GraphicsTriangleFixtureShader.cs");
        var compilation = CompilationBuilder.Build(source);

        var result = ShadeEmitter.Emit(compilation);

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Shaders.Keys, Is.EquivalentTo(new[] { "GraphicsTriangleFixtureShader" }));

        const string expected = """
                                 namespace Rin::Shade::Tests::Fixtures
                                 {
                                     struct GraphicsPushConstants
                                     {
                                         float4 color;
                                     }

                                     struct VsIn
                                     {
                                         int vertexId : SV_VertexID;
                                     }

                                     struct VsOut
                                     {
                                         float2 uv : UV;
                                         float4 position : SV_Position;
                                     }

                                     struct FsIn
                                     {
                                         float2 uv : UV;
                                     }

                                 }

                                 [[vk::push_constant]] uniform ConstantBuffer<Rin::Shade::Tests::Fixtures::GraphicsPushConstants, ScalarDataLayout> push;

                                 [shader("vertex")]
                                 Rin::Shade::Tests::Fixtures::VsOut vertex(Rin::Shade::Tests::Fixtures::VsIn input)
                                 {
                                     Rin::Shade::Tests::Fixtures::VsOut output;
                                     output.uv = float2(0, 0);
                                     output.position = float4(0, 0, 0, 1);
                                     return output;
                                 }

                                 [shader("fragment")]
                                 float4 fragment(Rin::Shade::Tests::Fixtures::FsIn input)
                                 {
                                     return push.color;
                                 }

                                 """;

        Assert.That(result.Shaders["GraphicsTriangleFixtureShader"].Replace("\r\n", "\n"), Is.EqualTo(expected.Replace("\r\n", "\n")));
    }

    [Test]
    public void AttachmentAttributesAreNotEmittedIntoSlang()
    {
        const string source = """
                               using System.Numerics;
                               using Rin.Shade;

                               namespace GBufferOutCheck;

                               public struct GBufferVsOut
                               {
                                   [Semantic("SV_Position")] public Vector4 Position;
                               }

                               public struct GBufferFsIn
                               {
                                   [Semantic("SV_Position")] public Vector4 Position;
                               }

                               public struct GBufferOut
                               {
                                   [Attachment(AttachmentFormat.RGBA32)] [Semantic("SV_Target1")] public Vector4 Color;
                                   [Attachment(AttachmentFormat.RGBA32)] [Semantic("SV_Target2")] public Vector4 Normal;
                               }

                               [Shader("Fixtures/gbuffer.slang")]
                               public class GBufferShader : Shader
                               {
                                   [Vertex]
                                   public GBufferVsOut Vertex()
                                   {
                                       GBufferVsOut output;
                                       output.Position = new Vector4(0f, 0f, 0f, 1f);
                                       return output;
                                   }

                                   [Fragment]
                                   public GBufferOut Fragment(GBufferFsIn input)
                                   {
                                       GBufferOut output;
                                       output.Color = new Vector4(1f, 0f, 0f, 1f);
                                       output.Normal = new Vector4(0f, 1f, 0f, 1f);
                                       return output;
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Shaders["GBufferShader"].Replace("\r\n", "\n"),
            Does.Not.Contain("Attachment"));
    }

    [Test]
    public void FragmentWithoutVertexIsRejected()
    {
        const string source = """
                               using System.Numerics;
                               using Rin.Shade;

                               namespace FragmentWithoutVertexCheck;

                               public struct FsOnlyIn
                               {
                                   [Semantic("UV")] public Vector2 Uv;
                               }

                               [Shader("Fixtures/fragment_only.slang")]
                               public class FragmentOnlyShader : Shader
                               {
                                   [Fragment]
                                   public Vector4 Fragment(FsOnlyIn input) => new Vector4(1f, 1f, 1f, 1f);
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics.Any(d => d.Id == Diagnostics.Emitter.FragmentWithoutVertex.Id), Is.True);
    }

    [Test]
    public void ComputeAndVertexTogetherIsRejected()
    {
        const string source = """
                               using Rin.Shade;

                               namespace ConflictingEntryPointsCheck;

                               public struct ConflictingVsOut
                               {
                                   [Semantic("SV_Position")] public System.Numerics.Vector4 Position;
                               }

                               public struct ConflictingVsIn
                               {
                                   [Semantic("SV_VertexID")] public int VertexId;
                               }

                               [Shader("Fixtures/conflicting.slang")]
                               public class ConflictingEntryPointsShader : Shader
                               {
                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                   }

                                   [Vertex]
                                   public ConflictingVsOut Vertex(ConflictingVsIn input)
                                   {
                                       ConflictingVsOut output;
                                       output.Position = new System.Numerics.Vector4(0f, 0f, 0f, 1f);
                                       return output;
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics.Any(d => d.Id == Diagnostics.Emitter.ConflictingEntryPoints.Id), Is.True);
    }

    [Test]
    public void DuplicateVertexEntryPointsAreRejected()
    {
        const string source = """
                               using System.Numerics;
                               using Rin.Shade;

                               namespace DuplicateVertexCheck;

                               public struct DupVsOut
                               {
                                   [Semantic("SV_Position")] public Vector4 Position;
                               }

                               [Shader("Fixtures/duplicate_vertex.slang")]
                               public class DuplicateVertexShader : Shader
                               {
                                   [Vertex]
                                   public DupVsOut VertexA()
                                   {
                                       DupVsOut output;
                                       output.Position = new Vector4(0f, 0f, 0f, 1f);
                                       return output;
                                   }

                                   [Vertex]
                                   public DupVsOut VertexB()
                                   {
                                       DupVsOut output;
                                       output.Position = new Vector4(0f, 0f, 0f, 1f);
                                       return output;
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics.Any(d => d.Id == Diagnostics.Emitter.DuplicateStageEntryPoint.Id), Is.True);
    }
}
