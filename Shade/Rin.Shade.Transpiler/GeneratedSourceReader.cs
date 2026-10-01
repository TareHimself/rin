using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Rin.Shade.Transpiler;

/// <summary>
/// Reads the shader source text that Rin.Shade.SourceGenerator embedded in a [ShaderSources]-marked
/// assembly as compile-time string constants, so no metadata parsing or assembly loading is needed.
/// </summary>
internal static class GeneratedSourceReader
{
    /// <summary>
    /// The embedded source texts of the referenced assembly, or nothing if it is not [ShaderSources]-marked.
    /// </summary>
    public static IEnumerable<string> ReadShaderSources(Compilation compilation, MetadataReference reference)
    {
        if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assembly) yield break;
        if (!assembly.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "Rin.Shade.ShaderSourcesAttribute"))
            yield break;

        var container = assembly.GetTypeByMetadataName("Rin.Shade.Generated.ShaderSourceContainer");
        if (container is null) yield break;

        foreach (var field in container.GetMembers().OfType<IFieldSymbol>())
        {
            if (field is { HasConstantValue: true, ConstantValue: string text })
                yield return text;
        }
    }
}
