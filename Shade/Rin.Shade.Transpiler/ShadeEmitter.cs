using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Rin.Shade.Transpiler;

/// <summary>
/// Entry point of the transpiler: finds the [Shader] classes in a compilation and lowers each to Slang.
/// </summary>
public static class ShadeEmitter
{
    /// <summary>
    /// Emits every [Shader] class in the compilation.
    /// </summary>
    public static ShadeEmitResult Emit(Compilation compilation) => Emit(compilation, compilation.SyntaxTrees);

    /// <summary>
    /// Emits the [Shader] classes declared in localTrees. Embedded trees from ScratchCompilationBuilder
    /// only make bodies walkable and are never entry points.
    /// </summary>
    public static ShadeEmitResult Emit(Compilation compilation, IEnumerable<SyntaxTree> localTrees)
    {
        var diagnostics = new List<Diagnostic>();
        var shaders = new Dictionary<string, string>();

        var shaderBaseType = compilation.GetTypeByMetadataName("Rin.Shade.Shader");
        if (shaderBaseType is null)
        {
            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.MissingShaderBaseType, Location.None));
            return new ShadeEmitResult(shaders, [..diagnostics]);
        }

        foreach (var tree in localTrees)
        {
            var model = compilation.GetSemanticModel(tree);
            foreach (var classDeclaration in tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                if (model.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol classSymbol) continue;
                if (!InheritsFrom(classSymbol, shaderBaseType)) continue;
                // An intermediate base class derives from Shader too but is not an emission target without [Shader].
                if (!HasShaderAttribute(classSymbol)) continue;

                var lowering = new ShaderLowering(compilation, diagnostics);
                var slang = lowering.Lower(classSymbol);
                if (slang is not null) shaders[classSymbol.Name] = slang;
            }
        }

        return new ShadeEmitResult(shaders, [..diagnostics]);
    }

    private static bool InheritsFrom(INamedTypeSymbol type, INamedTypeSymbol baseType)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
                return true;
        return false;
    }

    private static bool HasShaderAttribute(INamedTypeSymbol type) =>
        type.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "Rin.Shade.ShaderAttribute");
}

/// <summary>
/// The emitted Slang source keyed by shader class name, plus the diagnostics raised while emitting.
/// </summary>
public sealed record ShadeEmitResult(
    IReadOnlyDictionary<string, string> Shaders,
    ImmutableArray<Diagnostic> Diagnostics);
