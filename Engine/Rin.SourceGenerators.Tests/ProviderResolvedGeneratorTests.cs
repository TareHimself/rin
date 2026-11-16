using System.Linq;
using Microsoft.CodeAnalysis;
using Rin.Core.Graphics;
using Xunit;

namespace Rin.SourceGenerators.Tests;

public class ProviderResolvedGeneratorTests
{
    private const string Usings = @"
using Rin.Core.Graphics;
using Rin.Core.Shared.Providers;

namespace TestNamespace;
";

    private static GeneratorDriverRunResult Run(string moduleUser) =>
        GeneratorHarness.Run<ProviderResolvedGenerator>(Usings + moduleUser, typeof(IGraphicsModule));

    private static string GeneratedModuleUser(GeneratorDriverRunResult runResult) =>
        runResult.GeneratedTrees.Single(t => t.FilePath.Contains("ModuleUser")).GetText().ToString();

    [Fact]
    public void GeneratesGetOnlyResolvedProperty()
    {
        var runResult = Run(@"
public partial class ModuleUser
{
    [Resolved]
    public partial IGraphicsModule Graphics { get; }
}
");

        Assert.Empty(runResult.Diagnostics);

        var generatedText = GeneratedModuleUser(runResult);

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
        var runResult = Run(@"
public partial class ModuleUser
{
    [Resolved]
    public partial IGraphicsModule Graphics { get; set; }
}
");

        Assert.Empty(runResult.Diagnostics);

        var generatedText = GeneratedModuleUser(runResult);

        Assert.Contains(
            "get => field ??= global::Rin.Core.Global.Provider.Get<global::Rin.Core.Graphics.IGraphicsModule>();",
            generatedText);
        Assert.Contains("set;", generatedText);
    }

    [Theory]
    [InlineData("RIN00018", "class", "public partial IGraphicsModule Graphics { get; }")]
    [InlineData("RIN00019", "partial class", "public IGraphicsModule Graphics { get; }")]
    [InlineData("RIN00020", "partial class", "public partial IGraphicsModule Graphics { set; }")]
    [InlineData("RIN00021", "partial class", "public partial int Count { get; }")]
    [InlineData("RIN00022", "partial class", "public static partial IGraphicsModule Graphics { get; }")]
    public void InvalidResolvedPropertyIsRejected(string expectedDiagnostic, string typeKeywords, string property)
    {
        var runResult = Run($"public {typeKeywords} ModuleUser {{ [Resolved] {property} }}");

        Assert.Contains(runResult.Diagnostics, d => d.Id == expectedDiagnostic);
    }
}
