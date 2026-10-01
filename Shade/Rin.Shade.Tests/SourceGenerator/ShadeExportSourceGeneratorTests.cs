using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Rin.Shade.SourceGenerator;

namespace Rin.Shade.Tests.SourceGenerator;

/// <summary>
/// Drives ShadeExportSourceGenerator directly via CSharpGeneratorDriver against an in-memory
/// compilation - no disk, no built assembly, no cross-project build ordering. Regression coverage
/// for the record/record-struct predicate gap: the generator's syntax-node predicate originally only
/// matched ClassDeclarationSyntax/StructDeclarationSyntax, silently never seeing a `record struct`
/// (Roslyn represents that as its own RecordDeclarationSyntax), so [ShadeExport] on one was silently
/// a no-op - no diagnostic, no embedded source, just quietly missing.
/// </summary>
public class ShadeExportSourceGeneratorTests
{
    private static string RunAndGetContainerSource(string source)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .Append(MetadataReference.CreateFromFile(typeof(ShadeExportAttribute).Assembly.Location));

        var compilation = CSharpCompilation.Create("GeneratorProbe",
            [CSharpSyntaxTree.ParseText(source)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driver = CSharpGeneratorDriver.Create(new ShadeExportSourceGenerator());
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        Assert.That(diagnostics, Is.Empty);

        var generatedTree = outputCompilation.SyntaxTrees
            .FirstOrDefault(t => t.FilePath.EndsWith("ShaderSourceContainer.g.cs"));

        return generatedTree?.GetText().ToString() ?? "";
    }

    [Test]
    public void ExportedClassIsEmbedded()
    {
        const string source = """
                               using Rin.Shade;

                               namespace ExportCheck;

                               [ShadeExport]
                               public static class Helpers
                               {
                                   public static int One() => 1;
                               }
                               """;

        Assert.That(RunAndGetContainerSource(source), Does.Contain("public static int One() => 1;"));
    }

    [Test]
    public void ExportedRecordStructIsEmbedded()
    {
        const string source = """
                               using Rin.Shade;

                               namespace ExportCheck;

                               [ShadeExport]
                               public readonly record struct PackedThing
                               {
                                   private readonly uint _data;

                                   public PackedThing(uint data) => _data = data;

                                   public uint Id => _data & 0xFF;
                               }
                               """;

        Assert.That(RunAndGetContainerSource(source), Does.Contain("public uint Id => _data & 0xFF;"));
    }
}
