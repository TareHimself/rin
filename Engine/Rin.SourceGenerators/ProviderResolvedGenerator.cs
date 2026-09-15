using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Rin.SourceGenerators;

[Generator]
public class ProviderResolvedGenerator : IIncrementalGenerator
{
    private const string ResolvedAttributeFullName = "Rin.Core.Shared.Providers.ResolvedAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var properties = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is PropertyDeclarationSyntax { AttributeLists.Count: > 0 },
                transform: static (ctx, _) => GetPropertyForSourceGen(ctx))
            .Where(static p => p.propertyDeclaration is not null);

        context.RegisterSourceOutput(context.CompilationProvider.Combine(properties.Collect()),
            static (spc, source) => Execute(spc, source.Right));
    }

    private static (PropertyDeclarationSyntax? propertyDeclaration, IPropertySymbol? propertySymbol)
        GetPropertyForSourceGen(GeneratorSyntaxContext context)
    {
        var propertyDeclaration = (PropertyDeclarationSyntax)context.Node;

        foreach (var attributeList in propertyDeclaration.AttributeLists)
        foreach (var attribute in attributeList.Attributes)
        {
            if (ModelExtensions.GetSymbolInfo(context.SemanticModel, attribute).Symbol is IMethodSymbol
                {
                    ContainingType: { } attrType
                } &&
                attrType.ToDisplayString() == ResolvedAttributeFullName)
            {
                var propertySymbol =
                    ModelExtensions.GetDeclaredSymbol(context.SemanticModel, propertyDeclaration) as IPropertySymbol;
                return (propertyDeclaration, propertySymbol);
            }
        }

        return (null, null);
    }

    private static void Execute(SourceProductionContext context,
        ImmutableArray<(PropertyDeclarationSyntax? propertyDeclaration, IPropertySymbol? propertySymbol)> properties)
    {
        if (properties.IsDefaultOrEmpty) return;

        var byType =
            new Dictionary<INamedTypeSymbol, List<(PropertyDeclarationSyntax decl, IPropertySymbol symbol)>>(
                SymbolEqualityComparer.Default);

        foreach (var item in properties)
        {
            if (item.propertyDeclaration is not { } propertyDeclaration || item.propertySymbol is not { } propertySymbol)
                continue;

            var containingType = propertySymbol.ContainingType;

            if (propertySymbol.IsStatic)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.Providers.PropertyMustNotBeStatic,
                    propertyDeclaration.Identifier.GetLocation(),
                    propertySymbol.Name));
                continue;
            }

            if (!GeneratorUtils.IsPartial(containingType))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.Providers.ContainingTypeMustBePartial,
                    propertyDeclaration.Identifier.GetLocation(),
                    containingType.ToDisplayString(), propertySymbol.Name));
                continue;
            }

            // A valid partial-property *defining* declaration is bodyless: `partial T Foo { get; }` /
            // `partial T Foo { get; set; }`.
            if (!propertyDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword) ||
                propertyDeclaration.ExpressionBody is not null ||
                (propertyDeclaration.AccessorList?.Accessors
                    .Any(a => a.Body is not null || a.ExpressionBody is not null) ?? true))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.Providers.PropertyMustBePartial,
                    propertyDeclaration.Identifier.GetLocation(),
                    propertySymbol.Name));
                continue;
            }

            var accessors = propertyDeclaration.AccessorList!.Accessors;
            var hasGetter = accessors.Any(a => a.Kind() == SyntaxKind.GetAccessorDeclaration);
            var onlyGetOrSet = accessors.All(a =>
                a.Kind() is SyntaxKind.GetAccessorDeclaration or SyntaxKind.SetAccessorDeclaration);
            if (!hasGetter || !onlyGetOrSet)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.Providers.PropertyMustHaveGetter,
                    propertyDeclaration.Identifier.GetLocation(),
                    propertySymbol.Name));
                continue;
            }

            if (propertySymbol.Type.IsValueType)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.Providers.InvalidPropertyType,
                    propertyDeclaration.Identifier.GetLocation(),
                    propertySymbol.Name, propertySymbol.Type.ToDisplayString()));
                continue;
            }

            if (!byType.TryGetValue(containingType, out var list))
            {
                list = [];
                byType[containingType] = list;
            }

            list.Add((propertyDeclaration, propertySymbol));
        }

        foreach (var (containingType, propertyList) in byType)
            GenerateResolvedProperties(context, containingType, propertyList);
    }

    private static void GenerateResolvedProperties(SourceProductionContext context, INamedTypeSymbol containingType,
        List<(PropertyDeclarationSyntax decl, IPropertySymbol symbol)> properties)
    {
        var namespaceName = containingType.ContainingNamespace.ToDisplayString();
        var typeName = containingType.Name;
        var kind = containingType.IsValueType ? "struct" : "class";

        var output = new SourceBuilder();
        output
            .Line("// <auto-generated/>")
            .Line()
            .Line($"namespace {namespaceName};")
            .Line($"partial {kind} {typeName}")
            .OpenBrace();

        foreach (var (decl, property) in properties)
        {
            var propertyType = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var accessibility = GetAccessibilityModifier(property.DeclaredAccessibility);
            var hasSetter = decl.AccessorList!.Accessors.Any(a => a.Kind() == SyntaxKind.SetAccessorDeclaration);

            output.Line($"{accessibility} partial {propertyType} {property.Name}")
                .OpenBrace()
                .Line($"get => field ??= global::Rin.Core.Global.Provider.Get<{propertyType}>();");
            if (hasSetter) output.Line("set;");
            output.CloseBrace();
        }

        output.CloseBrace();

        context.AddSource($"Resolved_{GeneratorUtils.GetSafeHintName(containingType)}.g.cs", output.ToSourceText());
    }

    private static string GetAccessibilityModifier(Accessibility accessibility)
    {
        return accessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Private => "private",
            Accessibility.Protected => "protected",
            Accessibility.Internal => "internal",
            Accessibility.ProtectedOrInternal => "protected internal",
            Accessibility.ProtectedAndInternal => "private protected",
            _ => "private"
        };
    }
}
