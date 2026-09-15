using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Rin.SourceGenerators;
using Rin.Core.Graphics;
using Xunit;

namespace Rin.SourceGenerators.Tests;

public class ProviderResolvedGeneratorTests
{
    // The full set of reference assemblies (not just System.Private.CoreLib) is needed so custom
    // attribute base-type resolution (ResolvedAttribute -> Attribute) doesn't fail with CS0012.
    private static readonly MetadataReference[] References = ((string)AppContext
            .GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
        .Append(MetadataReference.CreateFromFile(typeof(IGraphicsModule).Assembly.Location))
        .ToArray();

    private static GeneratorDriverRunResult Run(string source)
    {
        var generator = new ProviderResolvedGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        var compilation = CSharpCompilation.Create(nameof(ProviderResolvedGeneratorTests),
            [CSharpSyntaxTree.ParseText(source)], References);
        return driver.RunGenerators(compilation).GetRunResult();
    }

    [Fact]
    public void GeneratesGetOnlyResolvedProperty()
    {
        const string source = @"
using Rin.Core.Graphics;
using Rin.Core.Shared.Providers;

namespace TestNamespace;

public partial class ModuleUser
{
    [Resolved]
    public partial IGraphicsModule Graphics { get; }
}
";
        var runResult = Run(source);

        Assert.Empty(runResult.Diagnostics);

        var generatedText = runResult.GeneratedTrees.Single(t => t.FilePath.Contains("ModuleUser")).GetText()
            .ToString();

        Assert.Contains(
            "public partial global::Rin.Core.Graphics.IGraphicsModule Graphics",
            generatedText);
        Assert.Contains(
            "get => field ??= global::Rin.Core.Global.Provider.Get<global::Rin.Core.Graphics.IGraphicsModule>();",
            generatedText);
        Assert.DoesNotContain("set;", generatedText);
    }

    [Fact]
    public void GeneratesSettableResolvedPropertyForTestOverride()
    {
        const string source = @"
using Rin.Core.Graphics;
using Rin.Core.Shared.Providers;

namespace TestNamespace;

public partial class ModuleUser
{
    [Resolved]
    public partial IGraphicsModule Graphics { get; set; }
}
";
        var runResult = Run(source);

        Assert.Empty(runResult.Diagnostics);

        var generatedText = runResult.GeneratedTrees.Single(t => t.FilePath.Contains("ModuleUser")).GetText()
            .ToString();

        Assert.Contains(
            "get => field ??= global::Rin.Core.Global.Provider.Get<global::Rin.Core.Graphics.IGraphicsModule>();",
            generatedText);
        Assert.Contains("set;", generatedText);
    }

    [Fact]
    public void ContainingTypeMustBePartial()
    {
        const string source = @"
using Rin.Core.Graphics;
using Rin.Core.Shared.Providers;

namespace TestNamespace;

public class ModuleUser
{
    [Resolved]
    public partial IGraphicsModule Graphics { get; }
}
";
        var runResult = Run(source);

        Assert.Contains(runResult.Diagnostics, d => d.Id == "RIN00018");
    }

    [Fact]
    public void PropertyMustBePartial()
    {
        const string source = @"
using Rin.Core.Graphics;
using Rin.Core.Shared.Providers;

namespace TestNamespace;

public partial class ModuleUser
{
    [Resolved]
    public IGraphicsModule Graphics { get; }
}
";
        var runResult = Run(source);

        Assert.Contains(runResult.Diagnostics, d => d.Id == "RIN00019");
    }

    [Fact]
    public void PropertyMustHaveGetter()
    {
        const string source = @"
using Rin.Core.Graphics;
using Rin.Core.Shared.Providers;

namespace TestNamespace;

public partial class ModuleUser
{
    [Resolved]
    public partial IGraphicsModule Graphics { set; }
}
";
        var runResult = Run(source);

        Assert.Contains(runResult.Diagnostics, d => d.Id == "RIN00020");
    }

    [Fact]
    public void PropertyMustBeReferenceType()
    {
        const string source = @"
using Rin.Core.Shared.Providers;

namespace TestNamespace;

public partial class ModuleUser
{
    [Resolved]
    public partial int Count { get; }
}
";
        var runResult = Run(source);

        Assert.Contains(runResult.Diagnostics, d => d.Id == "RIN00021");
    }

    [Fact]
    public void PropertyMustNotBeStatic()
    {
        const string source = @"
using Rin.Core.Graphics;
using Rin.Core.Shared.Providers;

namespace TestNamespace;

public partial class ModuleUser
{
    [Resolved]
    public static partial IGraphicsModule Graphics { get; }
}
";
        var runResult = Run(source);

        Assert.Contains(runResult.Diagnostics, d => d.Id == "RIN00022");
    }
}
