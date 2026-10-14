using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Rin.Shade.Transpiler;

/// <summary>
/// Builds one compilation out of a consumer's own local shader source plus the generated shader source
/// of its [ShaderSources]-marked references, so every shader is transpiled from C# source at the
/// consumer's build. The reference list is trusted to already be the full transitive set.
/// </summary>
public static class ScratchCompilationBuilder
{
    /// <summary>
    /// Parses the local sources, adds the embedded sources found in the references, and compiles them together.
    /// </summary>
    public static ScratchCompilationResult Build(IEnumerable<string> localSources,
        IEnumerable<MetadataReference> references, string name = "RinShadeScratch") =>
        Build(localSources.Select(source => new LocalSource("", source)), references, name);

    /// <summary>
    /// Like <see cref="Build(IEnumerable{string}, IEnumerable{MetadataReference}, string)" />, with each local
    /// source parsed under its file path so diagnostics and source maps can name the file.
    /// </summary>
    public static ScratchCompilationResult Build(IEnumerable<LocalSource> localSources,
        IEnumerable<MetadataReference> references, string name = "RinShadeScratch")
    {
        var referenceList = references.ToList();
        var localTrees = localSources
            .Select(source => (SyntaxTree)CSharpSyntaxTree.ParseText(source.Text, path: source.Path))
            .ToList();

        // Only used to resolve each reference's assembly symbol, which needs a Compilation to ask through.
        var probe = CSharpCompilation.Create(name, localTrees, referenceList,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var embeddedTrees = referenceList
            .SelectMany(reference => GeneratedSourceReader.ReadShaderSources(probe, reference))
            .Select(source => (SyntaxTree)CSharpSyntaxTree.ParseText(source))
            .ToList();

        // Embedded types also exist in the referenced assemblies; Roslyn reports CS0436 and picks the source symbol, as wanted.
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            .WithSpecificDiagnosticOptions(new Dictionary<string, ReportDiagnostic> { ["CS0436"] = ReportDiagnostic.Suppress });

        var compilation = CSharpCompilation.Create(name, localTrees.Concat(embeddedTrees), referenceList, options);

        return new ScratchCompilationResult(compilation, [..localTrees]);
    }
}

/// <summary>
/// One of the consumer's own source files: its path, for locations, and its text.
/// </summary>
public sealed record LocalSource(string Path, string Text);

/// <summary>
/// The scratch compilation and its LocalTrees, the subset of syntax trees that came from the consumer's
/// own source rather than an embedded reference. Pass LocalTrees to ShadeEmitter.Emit so entry points are
/// only searched for locally.
/// </summary>
public sealed record ScratchCompilationResult(Compilation Compilation, ImmutableArray<SyntaxTree> LocalTrees);
