using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Rin.Shade.Transpiler;

/// <summary>
/// Reads the source text a [ShaderSources]-marked assembly's Rin.Shade.SourceGenerator produced -
/// plain Roslyn constant-value reading (IFieldSymbol.ConstantValue), the exact same mechanism
/// already used everywhere else in this project to read [SlangCall] template strings off attribute
/// constructor arguments. No raw PE/metadata parsing, no Assembly.LoadFrom - the source generator
/// already did the work of turning file text into an ordinary compile-time constant.
/// </summary>
internal static class GeneratedSourceReader
{
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
