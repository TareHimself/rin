using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Rin.Shade.SourceGenerator;

namespace Rin.Shade.Tests.SourceGenerator;

public class ShaderDescriptorSourceGeneratorTests
{
    private static object GetDescriptor(string source, string typeName)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path));

        var compilation = CSharpCompilation.Create($"DescriptorProbe_{Guid.NewGuid():N}",
            [CSharpSyntaxTree.ParseText(source)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driver = CSharpGeneratorDriver.Create(new ShaderDescriptorSourceGenerator());
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);
        Assert.That(diagnostics, Is.Empty);

        using var stream = new MemoryStream();
        var emitResult = outputCompilation.Emit(stream);
        Assert.That(emitResult.Success, Is.True, string.Join("\n", emitResult.Diagnostics));

        stream.Position = 0;
        var assembly = AssemblyLoadContext.Default.LoadFromStream(stream);
        return assembly.GetType(typeName)!
            .GetProperty("Descriptor", BindingFlags.Public | BindingFlags.Static)!
            .GetValue(null)!;
    }

    [Test]
    public void DerivedShaderHidesTheBaseDescriptorAndPathExplicitly()
    {
        const string source = """
                               using Rin.Shade;

                               namespace HidingCheck;

                               [Shader("Check/base.slang")]
                               public partial class BaseShader : Shader
                               {
                                   [Compute(1, 1, 1)]
                                   public virtual void Compute() { }
                               }

                               [Shader("Check/derived.slang")]
                               public partial class DerivedShader : BaseShader
                               {
                                   public override void Compute() { }
                               }
                               """;

        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create($"HidingProbe_{Guid.NewGuid():N}",
            [CSharpSyntaxTree.ParseText(source)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driver = CSharpGeneratorDriver.Create(new ShaderDescriptorSourceGenerator(), new ShaderPathSourceGenerator());
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);

        var hiding = output.GetDiagnostics().Where(d => d.Id is "CS0108" or "CS0109").ToList();
        Assert.That(hiding, Is.Empty, string.Join(", ", hiding));
    }

    [Test]
    public void GraphicsShaderDescriptorCarriesPipelineState()
    {
        const string source = """
                               using System.Numerics;
                               using Rin.Shade;

                               namespace DescriptorCheck;

                               public struct Out
                               {
                                   [Semantic("SV_Position")] public Vector4 Position;
                               }

                               [Shader("Check/graphics.slang")]
                               public partial class GraphicsCheckShader : Shader
                               {
                                   protected override BlendState BlendState => BlendState.Additive;

                                   [Vertex]
                                   public virtual Out Vertex() => default;

                                   [Fragment, Attachment(AttachmentFormat.RGBA16), Depth, Stencil]
                                   public virtual Vector4 Fragment() => default;
                               }
                               """;

        var descriptor = (IGraphicsDescriptor)GetDescriptor(source, "DescriptorCheck.GraphicsCheckShader");

        Assert.That(descriptor.Path, Is.EqualTo("Check/graphics.slang"));
        Assert.That(descriptor.AttachmentFormats, Is.EqualTo(new[] { AttachmentFormat.RGBA16 }));
        Assert.That(descriptor.BlendState, Is.EqualTo(BlendState.Additive));
        Assert.That(descriptor.UsesDepth, Is.True);
        Assert.That(descriptor.UsesStencil, Is.True);
    }

    [Test]
    public void BlendStateOverrideInDerivedShaderWins()
    {
        const string source = """
                               using System.Numerics;
                               using Rin.Shade;

                               namespace DerivedBlendCheck;

                               public struct Out
                               {
                                   [Semantic("SV_Position")] public Vector4 Position;
                               }

                               public class UiBase : Shader
                               {
                                   protected override BlendState BlendState => BlendState.Alpha;

                                   [Vertex]
                                   public virtual Out Vertex() => default;

                                   [Fragment, Attachment(AttachmentFormat.RGBA8)]
                                   public virtual Vector4 Fragment() => default;
                               }

                               [Shader("Check/glow.slang")]
                               public partial class GlowShader : UiBase
                               {
                                   protected override BlendState BlendState => BlendState.Additive;
                               }
                               """;

        var descriptor = (IGraphicsDescriptor)GetDescriptor(source, "DerivedBlendCheck.GlowShader");

        Assert.That(descriptor.BlendState, Is.EqualTo(BlendState.Additive));
        Assert.That(descriptor.AttachmentFormats, Is.EqualTo(new[] { AttachmentFormat.RGBA8 }));
        Assert.That(descriptor.UsesDepth, Is.False);
    }

    [Test]
    public void PerFieldAttachmentsOnFragmentReturnStructAreCollectedInOrder()
    {
        const string source = """
                               using System.Numerics;
                               using Rin.Shade;

                               namespace GBufferDescriptorCheck;

                               public struct VsOut
                               {
                                   [Semantic("SV_Position")] public Vector4 Position;
                               }

                               public struct GBufferOut
                               {
                                   [Attachment(AttachmentFormat.RGBA32)] [Semantic("SV_Target0")] public Vector4 Color;
                                   [Attachment(AttachmentFormat.RGBA16)] [Semantic("SV_Target1")] public Vector4 Normal;
                               }

                               [Shader("Check/gbuffer.slang")]
                               public partial class GBufferCheckShader : Shader
                               {
                                   [Vertex]
                                   public VsOut Vertex() => default;

                                   [Fragment]
                                   public GBufferOut Fragment() => default;
                               }
                               """;

        var descriptor = (IGraphicsDescriptor)GetDescriptor(source, "GBufferDescriptorCheck.GBufferCheckShader");

        Assert.That(descriptor.AttachmentFormats,
            Is.EqualTo(new[] { AttachmentFormat.RGBA32, AttachmentFormat.RGBA16 }));
        Assert.That(descriptor.BlendState, Is.EqualTo(BlendState.None));
    }

    [Test]
    public void ComputeShaderDescriptorCarriesThreadGroupSize()
    {
        const string source = """
                               using Rin.Shade;

                               namespace ComputeDescriptorCheck;

                               [Shader("Check/compute.slang")]
                               public partial class ComputeCheckShader : Shader
                               {
                                   [Compute(8, 4, 2)]
                                   public virtual void Main()
                                   {
                                   }
                               }
                               """;

        var descriptor = (IComputeDescriptor)GetDescriptor(source, "ComputeDescriptorCheck.ComputeCheckShader");

        Assert.That(descriptor.Path, Is.EqualTo("Check/compute.slang"));
        Assert.That(descriptor.ThreadGroupSize, Is.EqualTo((8u, 4u, 2u)));
    }
}
