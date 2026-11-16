using System.Linq;
using Microsoft.CodeAnalysis;
using Rin.Core.Graphics;
using Xunit;

namespace Rin.SourceGenerators.Tests;

public class GraphicsShaderGeneratorTests
{
    private const string ShaderDefinitions = @"
using Rin.Core.Graphics;
using Rin.Core.Graphics.Shaders;

namespace TestNamespace;

[Rin.Shade.Shader]
public class PrettyShaderDefinition : Rin.Shade.Shader
{
}

[Rin.Shade.Shader]
public class BlurShaderDefinition : Rin.Shade.Shader
{
}

public class NotAShaderClass : Rin.Shade.Shader
{
}
";

    private static GeneratorDriverRunResult Run(string shaderUser) =>
        GeneratorHarness.Run<GraphicsShaderGenerator>(ShaderDefinitions + shaderUser, typeof(IGraphicsModule),
            typeof(Rin.Shade.Shader));

    [Fact]
    public void GeneratesShaderProperties()
    {
        var runResult = Run(@"
public partial class ShaderUser
{
    [GraphicsShader<PrettyShaderDefinition>]
    public partial IGraphicsShader PrettyShader { get; }

    [ComputeShader<BlurShaderDefinition>]
    private partial IComputeShader BlurShader { get; }
}
");

        Assert.Empty(runResult.Diagnostics);

        var generatedFileSyntax = runResult.GeneratedTrees.Single(t => t.FilePath.Contains("ShaderUser"));
        var generatedText = generatedFileSyntax.GetText().ToString();

        Assert.Contains(
            "public partial global::Rin.Core.Graphics.Shaders.IGraphicsShader PrettyShader => field ??= global::Rin.Core.Graphics.IGraphicsModule.Get().MakeGraphics(global::TestNamespace.PrettyShaderDefinition.Descriptor);",
            generatedText);
        Assert.Contains(
            "private partial global::Rin.Core.Graphics.Shaders.IComputeShader BlurShader => field ??= global::Rin.Core.Graphics.IGraphicsModule.Get().MakeCompute(global::TestNamespace.BlurShaderDefinition.Descriptor);",
            generatedText);
    }

    [Theory]
    [InlineData("RIN00013", "class",
        "[GraphicsShader<PrettyShaderDefinition>] public partial IGraphicsShader PrettyShader { get; }")]
    [InlineData("RIN00014", "partial class",
        "[GraphicsShader<PrettyShaderDefinition>] public IGraphicsShader PrettyShader { get; }")]
    [InlineData("RIN00015", "partial class",
        "[GraphicsShader<PrettyShaderDefinition>] public partial IGraphicsShader PrettyShader { get; set; }")]
    [InlineData("RIN00016", "partial class",
        "[GraphicsShader<PrettyShaderDefinition>] public partial string PrettyShader { get; }")]
    [InlineData("RIN00017", "partial class",
        "[GraphicsShader<PrettyShaderDefinition>] public static partial IGraphicsShader PrettyShader { get; }")]
    [InlineData("RIN00023", "partial class",
        "[ComputeShader<NotAShaderClass>] private partial IComputeShader BlurShader { get; }")]
    [InlineData("RIN00012", "partial class",
        "[GraphicsShader<PrettyShaderDefinition>] [ComputeShader<BlurShaderDefinition>] public partial IGraphicsShader PrettyShader { get; }")]
    public void InvalidShaderPropertyIsRejected(string expectedDiagnostic, string typeKeywords, string property)
    {
        var runResult = Run($"public {typeKeywords} ShaderUser {{ {property} }}");

        Assert.Contains(runResult.Diagnostics, d => d.Id == expectedDiagnostic);
    }
}
