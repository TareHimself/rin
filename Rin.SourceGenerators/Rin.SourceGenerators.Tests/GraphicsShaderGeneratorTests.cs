using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Rin.Core.Generators;
using Rin.Core.Graphics;
using Xunit;

namespace Rin.SourceGenerators.Tests;

public class GraphicsShaderGeneratorTests
{
    // The full set of reference assemblies (not just System.Private.CoreLib) is needed so custom
    // attribute base-type resolution (ShaderAttribute -> Attribute) doesn't fail with CS0012.
    private static readonly MetadataReference[] References = ((string)AppContext
            .GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
        .Append(MetadataReference.CreateFromFile(typeof(IGraphicsModule).Assembly.Location))
        .ToArray();

    private static GeneratorDriverRunResult Run(string source)
    {
        var generator = new GraphicsShaderGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        var compilation = CSharpCompilation.Create(nameof(GraphicsShaderGeneratorTests),
            [CSharpSyntaxTree.ParseText(source)], References);
        return driver.RunGenerators(compilation).GetRunResult();
    }

    [Fact]
    public void GeneratesShaderProperties()
    {
        const string source = @"
using Rin.Core.Graphics;
using Rin.Core.Graphics.Shaders;

namespace TestNamespace;

public partial class ShaderUser
{
    [GraphicsShader(""fs/pretty.slang"")]
    public partial IGraphicsShader PrettyShader { get; }

    [ComputeShader(""cs/blur.slang"")]
    private partial IComputeShader BlurShader { get; }
}
";
        var runResult = Run(source);

        Assert.Empty(runResult.Diagnostics);

        var generatedFileSyntax = runResult.GeneratedTrees.Single(t => t.FilePath.Contains("ShaderUser"));
        var generatedText = generatedFileSyntax.GetText().ToString();

        Assert.Contains(
            "public partial global::Rin.Core.Graphics.Shaders.IGraphicsShader PrettyShader => field ??= global::Rin.Core.Graphics.IGraphicsModule.Get().MakeGraphics(\"fs/pretty.slang\");",
            generatedText);
        Assert.Contains(
            "private partial global::Rin.Core.Graphics.Shaders.IComputeShader BlurShader => field ??= global::Rin.Core.Graphics.IGraphicsModule.Get().MakeCompute(\"cs/blur.slang\");",
            generatedText);
    }

    [Fact]
    public void ContainingTypeMustBePartial()
    {
        const string source = @"
using Rin.Core.Graphics.Shaders;

namespace TestNamespace;

public class ShaderUser
{
    [GraphicsShader(""fs/pretty.slang"")]
    public partial IGraphicsShader PrettyShader { get; }
}
";
        var runResult = Run(source);

        Assert.Contains(runResult.Diagnostics, d => d.Id == "RIN00013");
    }

    [Fact]
    public void PropertyMustBePartial()
    {
        const string source = @"
using Rin.Core.Graphics.Shaders;

namespace TestNamespace;

public partial class ShaderUser
{
    [GraphicsShader(""fs/pretty.slang"")]
    public IGraphicsShader PrettyShader { get; }
}
";
        var runResult = Run(source);

        Assert.Contains(runResult.Diagnostics, d => d.Id == "RIN00014");
    }

    [Fact]
    public void PropertyMustBeGetOnly()
    {
        const string source = @"
using Rin.Core.Graphics.Shaders;

namespace TestNamespace;

public partial class ShaderUser
{
    [GraphicsShader(""fs/pretty.slang"")]
    public partial IGraphicsShader PrettyShader { get; set; }
}
";
        var runResult = Run(source);

        Assert.Contains(runResult.Diagnostics, d => d.Id == "RIN00015");
    }

    [Fact]
    public void PropertyTypeMustMatchAttribute()
    {
        const string source = @"
using Rin.Core.Graphics.Shaders;

namespace TestNamespace;

public partial class ShaderUser
{
    [GraphicsShader(""fs/pretty.slang"")]
    public partial string PrettyShader { get; }
}
";
        var runResult = Run(source);

        Assert.Contains(runResult.Diagnostics, d => d.Id == "RIN00016");
    }

    [Fact]
    public void PropertyMustNotBeStatic()
    {
        const string source = @"
using Rin.Core.Graphics.Shaders;

namespace TestNamespace;

public partial class ShaderUser
{
    [GraphicsShader(""fs/pretty.slang"")]
    public static partial IGraphicsShader PrettyShader { get; }
}
";
        var runResult = Run(source);

        Assert.Contains(runResult.Diagnostics, d => d.Id == "RIN00017");
    }

    [Fact]
    public void AmbiguousShaderAttributeIsRejected()
    {
        const string source = @"
using Rin.Core.Graphics.Shaders;

namespace TestNamespace;

public partial class ShaderUser
{
    [GraphicsShader(""fs/pretty.slang"")]
    [ComputeShader(""cs/blur.slang"")]
    public partial IGraphicsShader PrettyShader { get; }
}
";
        var runResult = Run(source);

        Assert.Contains(runResult.Diagnostics, d => d.Id == "RIN00012");
    }
}
