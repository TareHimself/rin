using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Rin.Shade.Transpiler;

/// <summary>
/// Builds one compilation out of a consumer's own local shader source plus every generated shader
/// source pulled from its [ShaderSources]-marked references - "walk the full tree every time, from
/// C# source, at the consumer's build" per the design doc, never a cached/pre-transpiled artifact.
/// The consumer's own reference list is trusted to already be the full transitive set (true for a
/// real project-reference build, and for an explicit -r list that includes every relevant DLL) - no
/// separate transitive-assembly-resolution step is needed here.
/// </summary>
public static class ScratchCompilationBuilder
{
    public static ScratchCompilationResult Build(IEnumerable<string> localSources,
        IEnumerable<MetadataReference> references, string name = "RinShadeScratch")
    {
        var referenceList = references.ToList();
        var localTrees = localSources.Select(source => (SyntaxTree)CSharpSyntaxTree.ParseText(source)).ToList();

        // A bare compilation, just to resolve each reference's assembly symbol and check it for the
        // [ShaderSources] marker - IAssemblySymbol resolution needs a Compilation to ask through.
        var probe = CSharpCompilation.Create(name, localTrees, referenceList,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var embeddedTrees = referenceList
            .SelectMany(reference => GeneratedSourceReader.ReadShaderSources(probe, reference))
            .Select(source => (SyntaxTree)CSharpSyntaxTree.ParseText(source))
            .ToList();

        // The scratch compilation references the same assemblies it also embeds source from, so a
        // symbol exists as both a source declaration and an imported metadata one - Roslyn reports
        // that as CS0436 and picks the source symbol, which is exactly what's wanted here.
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            .WithSpecificDiagnosticOptions(new Dictionary<string, ReportDiagnostic> { ["CS0436"] = ReportDiagnostic.Suppress });

        var compilation = CSharpCompilation.Create(name, localTrees.Concat(embeddedTrees), referenceList, options);

        return new ScratchCompilationResult(compilation, [..localTrees]);
    }
}

/// <summary>
/// LocalTrees is the subset of Compilation.SyntaxTrees that came from the consumer's own source,
/// not from an embedded reference - pass it to ShadeEmitter.Emit so entry-point search stays
/// restricted to local trees per the design doc, even though body-resolution uses the whole
/// Compilation (local + embedded).
/// </summary>
public sealed record ScratchCompilationResult(Compilation Compilation, ImmutableArray<SyntaxTree> LocalTrees);
