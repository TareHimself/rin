using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Rin.Shade.SourceGenerator;

/// <summary>
/// Emits a `public static IGraphicsDescriptor/IComputeDescriptor Descriptor` on every [Shader]
/// class: the pipeline state a backend needs (attachment formats, blend state, depth/stencil,
/// thread-group size), read straight off the class's own attributes and overrides so no backend
/// has to recover it from compiled reflection. BlendState is evaluated by calling the shader's own
/// BlendState property, so any expression an override can write is supported. Silently skips
/// classes SHADEGEN0001/0002 already reject, and classes with no compute or vertex entry point.
/// </summary>
[Generator]
public class ShaderDescriptorSourceGenerator : IIncrementalGenerator
{
    private const string ShaderAttributeFullName = "Rin.Shade.ShaderAttribute";
    private const string ShaderBaseFullName = "Rin.Shade.Shader";
    private const string ComputeAttributeFullName = "Rin.Shade.ComputeAttribute";
    private const string VertexAttributeFullName = "Rin.Shade.VertexAttribute";
    private const string FragmentAttributeFullName = "Rin.Shade.FragmentAttribute";
    private const string AttachmentAttributeFullName = "Rin.Shade.AttachmentAttribute";
    private const string DepthAttributeFullName = "Rin.Shade.DepthAttribute";
    private const string StencilAttributeFullName = "Rin.Shade.StencilAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var shaderClasses = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
                transform: static (ctx, _) => GetShaderInfo(ctx))
            .Where(static info => info is not null);

        context.RegisterSourceOutput(shaderClasses, static (spc, info) => Execute(spc, info!.Value));
    }

    private static (INamedTypeSymbol Symbol, string Path)? GetShaderInfo(GeneratorSyntaxContext context)
    {
        var declaration = (ClassDeclarationSyntax)context.Node;
        var symbol = context.SemanticModel.GetDeclaredSymbol(declaration);
        if (symbol is null) return null;

        var attribute = symbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == ShaderAttributeFullName);
        return attribute?.ConstructorArguments.FirstOrDefault().Value is string path ? (symbol, path) : null;
    }

    private static void Execute(SourceProductionContext context, (INamedTypeSymbol Symbol, string Path) info)
    {
        var symbol = info.Symbol;
        if (symbol.ContainingType is not null || symbol.IsAbstract || symbol.IsGenericType) return;

        var isPartial = symbol.DeclaringSyntaxReferences
            .Select(r => r.GetSyntax())
            .OfType<ClassDeclarationSyntax>()
            .Any(d => d.Modifiers.Any(SyntaxKind.PartialKeyword));
        if (!isPartial) return;

        var pathLiteral = SymbolDisplay.FormatLiteral(info.Path, quote: true);
        var chain = WalkBaseChain(symbol);
        var entries = EffectiveMethods(chain).ToList();

        var compute = FindMarker(entries, ComputeAttributeFullName);
        var vertex = FindMarker(entries, VertexAttributeFullName);
        var fragment = FindMarker(entries, FragmentAttributeFullName);

        string descriptor;
        if (compute is { } computeEntry && vertex is null)
        {
            var args = computeEntry.Attribute.ConstructorArguments;
            if (args.Length != 3) return;
            descriptor =
                $"public {(BaseHasGeneratedDescriptor(symbol) ? "new " : "")}static global::Rin.Shade.IComputeDescriptor Descriptor {{ get; }} = new global::Rin.Shade.ComputeDescriptor({pathLiteral}, ({args[0].Value}u, {args[1].Value}u, {args[2].Value}u));";
        }
        else if (vertex is { } vertexEntry && compute is null)
        {
            var declSites = new List<IMethodSymbol> { vertexEntry.DeclSite };
            if (fragment is { } fragmentDeclEntry) declSites.Add(fragmentDeclEntry.DeclSite);

            var usesDepth = declSites.Any(m => HasAttribute(m, DepthAttributeFullName));
            var usesStencil = declSites.Any(m => HasAttribute(m, StencilAttributeFullName));
            var formats = fragment is { } fragmentEntry ? GetAttachmentFormats(fragmentEntry) : [];
            var formatsLiteral = string.Join(", ", formats.Select(f => $"global::Rin.Shade.AttachmentFormat.{f}"));

            descriptor =
                $"public {(BaseHasGeneratedDescriptor(symbol) ? "new " : "")}static global::Rin.Shade.IGraphicsDescriptor Descriptor {{ get; }} = new global::Rin.Shade.GraphicsDescriptor({pathLiteral}, new global::Rin.Shade.AttachmentFormat[] {{ {formatsLiteral} }}, new {symbol.Name}().BlendState, {(usesDepth ? "true" : "false")}, {(usesStencil ? "true" : "false")});";
        }
        else
        {
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated/>");
        if (!symbol.ContainingNamespace.IsGlobalNamespace)
            builder.AppendLine($"namespace {symbol.ContainingNamespace.ToDisplayString()};");
        builder.AppendLine();
        builder.AppendLine($"partial class {symbol.Name}");
        builder.AppendLine("{");
        builder.AppendLine($"    {descriptor}");
        builder.AppendLine("}");

        context.AddSource($"{symbol.Name}.ShaderDescriptor.g.cs", builder.ToString());
    }

    // A base shader that also gets a generated Descriptor is hidden by the derived one's, so it says `new`.
    private static bool BaseHasGeneratedDescriptor(INamedTypeSymbol symbol)
    {
        for (var current = symbol.BaseType; current is not null; current = current.BaseType)
            if (HasAttribute(current, ShaderAttributeFullName) && current is
                    { IsAbstract: false, IsGenericType: false, ContainingType: null } &&
                current.DeclaringSyntaxReferences.Select(r => r.GetSyntax()).OfType<ClassDeclarationSyntax>()
                    .Any(d => d.Modifiers.Any(SyntaxKind.PartialKeyword)))
                return true;

        return false;
    }

    private static List<INamedTypeSymbol> WalkBaseChain(INamedTypeSymbol shaderClass)
    {
        var chain = new List<INamedTypeSymbol>();
        for (var current = shaderClass;
             current is not null && current.ToDisplayString() != ShaderBaseFullName;
             current = current.BaseType)
            chain.Add(current);
        return chain;
    }

    private static IEnumerable<IMethodSymbol> EffectiveMethods(List<INamedTypeSymbol> chain)
    {
        var all = chain.SelectMany(t => t.GetMembers().OfType<IMethodSymbol>()).ToList();
        var shadowed = new HashSet<IMethodSymbol>(
            all.Select(m => m.OverriddenMethod).Where(m => m is not null)!,
            SymbolEqualityComparer.Default);
        return all.Where(m => !shadowed.Contains(m));
    }

    private static (IMethodSymbol DeclSite, AttributeData Attribute)? FindMarker(
        IEnumerable<IMethodSymbol> effectiveMethods, string attributeFullName)
    {
        foreach (var method in effectiveMethods)
        {
            for (var current = method; current is not null; current = current.OverriddenMethod)
            {
                var attribute = current.GetAttributes()
                    .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == attributeFullName);
                if (attribute is not null) return (current, attribute);
            }
        }

        return null;
    }

    private static bool HasAttribute(ISymbol symbol, string fullName) =>
        symbol.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == fullName);

    private static List<string> GetAttachmentFormats((IMethodSymbol DeclSite, AttributeData Attribute) fragment)
    {
        var formats = new List<string>();

        if (GetFormatName(fragment.DeclSite.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == AttachmentAttributeFullName)) is { } single)
        {
            formats.Add(single);
            return formats;
        }

        if (fragment.DeclSite.ReturnType is INamedTypeSymbol { TypeKind: TypeKind.Struct } returnStruct)
            foreach (var field in returnStruct.GetMembers().OfType<IFieldSymbol>())
                if (GetFormatName(field.GetAttributes()
                        .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == AttachmentAttributeFullName)) is
                    { } fieldFormat)
                    formats.Add(fieldFormat);

        return formats;
    }

    private static string? GetFormatName(AttributeData? attribute)
    {
        if (attribute is null) return null;

        var argument = attribute.ConstructorArguments.FirstOrDefault();
        if (argument.Value is not int value || argument.Type is not INamedTypeSymbol enumType) return null;

        return enumType.GetMembers().OfType<IFieldSymbol>()
            .FirstOrDefault(f => f.HasConstantValue && f.ConstantValue is int v && v == value)?.Name;
    }
}
