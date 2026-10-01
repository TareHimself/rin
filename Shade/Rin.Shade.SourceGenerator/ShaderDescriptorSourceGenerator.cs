using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Rin.Shade.SourceGenerator;

/// <summary>
/// Emits a static <c>Descriptor</c> on every partial <c>[Shader]</c> class, holding the pipeline
/// state a backend needs (attachment formats, blend state, depth and stencil use, thread-group
/// size), read from the class's attributes and overrides. The shader is never instantiated: the
/// expression of the most-derived <c>BlendState</c> override is copied into the descriptor along
/// with the using directives of its file. Classes that SHADEGEN0001 or SHADEGEN0002 reject, and
/// classes with no compute or vertex entry point, are skipped.
/// </summary>
[Generator]
public class ShaderDescriptorSourceGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor UnsupportedBlendState = new(
        id: "SHADEGEN0003",
        title: "Unsupported BlendState override",
        messageFormat: "The BlendState override of '{0}' must be an expression-bodied property, because the generated descriptor copies its expression instead of creating the shader",
        category: "Rin.Shade",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

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
        var usings = "";
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
            var attachments = fragment is { } fragmentEntry ? GetAttachments(fragmentEntry) : [];
            var formatsLiteral = string.Join(", ", attachments.Select(a => $"global::Rin.Shade.AttachmentFormat.{a.Format}"));
            var modifier = BaseHasGeneratedDescriptor(symbol) ? "new " : "";
            var blendState = FindBlendState(chain, out var blendUsings);
            if (blendState is null)
            {
                context.ReportDiagnostic(Diagnostic.Create(UnsupportedBlendState, symbol.Locations.FirstOrDefault(),
                    symbol.Name));
                return;
            }

            usings = blendUsings;
            var properties = string.Concat(attachments.Select((a, index) =>
                $"            /// <summary>The format of the {(a.Name.Length == 0 ? "fragment" : a.Name)} color attachment.</summary>\n" +
                $"            public global::Rin.Shade.AttachmentFormat {a.Name}Format => _formats[{index}];\n"));

            descriptor =
                $"/// <summary>The descriptor of {symbol.Name}, with the format of each color attachment under <c>Output</c>.</summary>\n" +
                $"    public {modifier}sealed record GeneratedDescriptor() : global::Rin.Shade.GraphicsDescriptor({pathLiteral}, new global::Rin.Shade.AttachmentFormat[] {{ {formatsLiteral} }}, {blendState}, {(usesDepth ? "true" : "false")}, {(usesStencil ? "true" : "false")})\n" +
                "    {\n" +
                "        /// <summary>The formats of the color attachments the fragment stage writes.</summary>\n" +
                "        public readonly struct OutputFormats(global::Rin.Shade.AttachmentFormat[] formats)\n" +
                "        {\n" +
                "            private readonly global::Rin.Shade.AttachmentFormat[] _formats = formats;\n" + properties +
                "        }\n" +
                "\n" +
                "        /// <summary>The color attachment formats.</summary>\n" +
                "        public OutputFormats Output => new(AttachmentFormats);\n" +
                "    }\n" +
                "\n" +
                $"    /// <summary>The compiled-shader descriptor for {symbol.Name}.</summary>\n" +
                $"    public {modifier}static GeneratedDescriptor Descriptor {{ get; }} = new();";
        }
        else
        {
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated/>");
        if (usings.Length > 0) builder.AppendLine(usings);
        if (!symbol.ContainingNamespace.IsGlobalNamespace)
            builder.AppendLine($"namespace {symbol.ContainingNamespace.ToDisplayString()};");
        builder.AppendLine();
        builder.AppendLine($"partial class {symbol.Name}");
        builder.AppendLine("{");
        builder.AppendLine($"    {descriptor}");
        builder.AppendLine("}");

        context.AddSource($"{symbol.Name}.ShaderDescriptor.g.cs", builder.ToString());
    }

    private static string? FindBlendState(List<INamedTypeSymbol> chain, out string usings)
    {
        usings = "";
        foreach (var type in chain)
        {
            var property = type.GetMembers().OfType<IPropertySymbol>().FirstOrDefault(p => p.Name == "BlendState");
            if (property is null) continue;

            var declaration = FindPropertySyntax(type, property);
            if (declaration?.ExpressionBody is not { } body) return null;

            usings = string.Join("\n", declaration.SyntaxTree.GetCompilationUnitRoot().Usings.Select(u => u.ToString()));
            return body.Expression.ToString();
        }

        return "global::Rin.Shade.BlendState.None";
    }

    private static PropertyDeclarationSyntax? FindPropertySyntax(INamedTypeSymbol type, IPropertySymbol property)
    {
        if (property.OriginalDefinition.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is
            PropertyDeclarationSyntax local)
            return local;

        return ReadExportedSource(type.OriginalDefinition)?.GetCompilationUnitRoot().DescendantNodes()
            .OfType<ClassDeclarationSyntax>().FirstOrDefault(c => c.Identifier.Text == type.Name)?.Members
            .OfType<PropertyDeclarationSyntax>().FirstOrDefault(p => p.Identifier.Text == "BlendState");
    }

    private static SyntaxTree? ReadExportedSource(INamedTypeSymbol type)
    {
        var container = type.ContainingAssembly.GetTypeByMetadataName("Rin.Shade.Generated.ShaderSourceContainer");
        var field = container?.GetMembers(type.Name).OfType<IFieldSymbol>().FirstOrDefault();
        return field is { HasConstantValue: true, ConstantValue: string text } ? CSharpSyntaxTree.ParseText(text) : null;
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

    private static List<(string Name, string Format)> GetAttachments((IMethodSymbol DeclSite, AttributeData Attribute) fragment)
    {
        var attachments = new List<(string Name, string Format)>();

        if (GetFormatName(fragment.DeclSite.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == AttachmentAttributeFullName)) is { } single)
        {
            attachments.Add(("", single));
            return attachments;
        }

        if (fragment.DeclSite.ReturnType is INamedTypeSymbol { TypeKind: TypeKind.Struct } returnStruct)
            foreach (var field in returnStruct.GetMembers().OfType<IFieldSymbol>())
                if (GetFormatName(field.GetAttributes()
                        .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == AttachmentAttributeFullName)) is
                    { } fieldFormat)
                    attachments.Add((field.Name, fieldFormat));

        return attachments;
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
