using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Rin.Shade.Transpiler;

public static class ShadeEmitter
{
    public static ShadeEmitResult Emit(Compilation compilation) => Emit(compilation, compilation.SyntaxTrees);

    /// <summary>
    /// localTrees restricts which trees are searched for [Shader]-attributed entry-point classes -
    /// per the design doc, embedded trees pulled in by ScratchCompilationBuilder exist purely to
    /// make bodies walkable, never as entry points of their own.
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
                // [Shader(...)] marks an emission target with its own output path - an intermediate
                // base class meant only to be derived from (walkable via the base-chain flattening
                // in ShaderLowering) isn't one on its own, even though it also derives from Shader.
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

public sealed record ShadeEmitResult(
    IReadOnlyDictionary<string, string> Shaders,
    ImmutableArray<Diagnostic> Diagnostics);
