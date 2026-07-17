using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Rin.Core.Generators;

[Generator]
public class GraphicsShaderGenerator : IIncrementalGenerator
{
    private const string ShaderAttributeFullName = "Rin.Core.Graphics.Shaders.ShaderAttribute";
    private const string GraphicsShaderAttributeFullName = "Rin.Core.Graphics.Shaders.GraphicsShaderAttribute";
    private const string ComputeShaderAttributeFullName = "Rin.Core.Graphics.Shaders.ComputeShaderAttribute";
    private const string GraphicsShaderInterfaceFullName = "Rin.Core.Graphics.Shaders.IGraphicsShader";
    private const string ComputeShaderInterfaceFullName = "Rin.Core.Graphics.Shaders.IComputeShader";
    private const string GraphicsModuleFullName = "Rin.Core.Graphics.IGraphicsModule";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Filter for properties that might carry a [GraphicsShader]/[ComputeShader] attribute.
        var properties = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is PropertyDeclarationSyntax { AttributeLists.Count: > 0 },
                transform: static (ctx, _) => GetPropertyForSourceGen(ctx))
            .Where(static p => p.propertyDeclaration is not null);

        context.RegisterSourceOutput(context.CompilationProvider.Combine(properties.Collect()),
            static (spc, source) => Execute(spc, source.Left, source.Right));
    }

    private static bool InheritsFromOrEquals(INamedTypeSymbol? type, string fullName)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.ToDisplayString() == fullName) return true;
        }

        return false;
    }

    /// <summary>
    /// Checks whether a property declaration carries one or more attributes deriving from
    /// <c>ShaderAttribute</c>, using semantic analysis to avoid string matching pitfalls.
    /// </summary>
    private static (PropertyDeclarationSyntax? propertyDeclaration, ImmutableArray<INamedTypeSymbol> attributeClasses)
        GetPropertyForSourceGen(GeneratorSyntaxContext context)
    {
        var propertyDeclaration = (PropertyDeclarationSyntax)context.Node;
        var matches = ImmutableArray.CreateBuilder<INamedTypeSymbol>();

        foreach (var attributeList in propertyDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                if (ModelExtensions.GetSymbolInfo(context.SemanticModel, attribute).Symbol is IMethodSymbol
                    {
                        ContainingType: var attrType
                    } &&
                    InheritsFromOrEquals(attrType, ShaderAttributeFullName))
                {
                    matches.Add(attrType);
                }
            }
        }

        return matches.Count > 0
            ? (propertyDeclaration, matches.ToImmutable())
            : (null, ImmutableArray<INamedTypeSymbol>.Empty);
    }

    private static void Execute(SourceProductionContext context, Compilation compilation,
        ImmutableArray<(PropertyDeclarationSyntax? propertyDeclaration, ImmutableArray<INamedTypeSymbol> attributeClasses
            )> properties)
    {
        if (properties.IsDefaultOrEmpty) return;

        var graphicsShaderType = compilation.GetTypeByMetadataName(GraphicsShaderInterfaceFullName);
        var computeShaderType = compilation.GetTypeByMetadataName(ComputeShaderInterfaceFullName);
        var graphicsModuleType = compilation.GetTypeByMetadataName(GraphicsModuleFullName);
        if (graphicsShaderType == null || computeShaderType == null || graphicsModuleType == null) return;

        var byType =
            new Dictionary<INamedTypeSymbol, List<(IPropertySymbol property, INamedTypeSymbol attributeClass)>>(
                SymbolEqualityComparer.Default);

        foreach (var item in properties)
        {
            if (item.propertyDeclaration is not { } propertyDeclaration) continue;

            if (item.attributeClasses.Length > 1)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.Shader.AmbiguousShaderAttribute,
                    propertyDeclaration.Identifier.GetLocation(),
                    propertyDeclaration.Identifier.Text));
                continue;
            }

            var semanticModel = compilation.GetSemanticModel(propertyDeclaration.SyntaxTree);
            if (ModelExtensions.GetDeclaredSymbol(semanticModel, propertyDeclaration) is not IPropertySymbol
                propertySymbol)
                continue;

            var attributeClass = item.attributeClasses[0];
            var containingType = propertySymbol.ContainingType;

            if (propertySymbol.IsStatic)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.Shader.PropertyMustNotBeStatic,
                    propertyDeclaration.Identifier.GetLocation(),
                    propertySymbol.Name));
                continue;
            }

            if (!GeneratorUtils.IsPartial(containingType))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.Shader.ContainingTypeMustBePartial,
                    propertyDeclaration.Identifier.GetLocation(),
                    containingType.ToDisplayString(), propertySymbol.Name));
                continue;
            }

            // A valid partial-property *defining* declaration is bodyless: `partial T Foo { get; }`.
            if (!propertyDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword) ||
                propertyDeclaration.ExpressionBody is not null ||
                (propertyDeclaration.AccessorList?.Accessors
                    .Any(a => a.Body is not null || a.ExpressionBody is not null) ?? true))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.Shader.PropertyMustBePartial,
                    propertyDeclaration.Identifier.GetLocation(),
                    propertySymbol.Name));
                continue;
            }

            var accessors = propertyDeclaration.AccessorList!.Accessors;
            if (accessors.Count != 1 || accessors[0].Kind() != SyntaxKind.GetAccessorDeclaration)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.Shader.PropertyMustBeGetOnly,
                    propertyDeclaration.Identifier.GetLocation(),
                    propertySymbol.Name));
                continue;
            }

            var isGraphics = attributeClass.ToDisplayString() == GraphicsShaderAttributeFullName;
            var isCompute = attributeClass.ToDisplayString() == ComputeShaderAttributeFullName;
            var expectedType = isGraphics ? graphicsShaderType : isCompute ? computeShaderType : null;
            if (expectedType == null) continue; // A ShaderAttribute-derived type we don't know how to emit for.

            if (!SymbolEqualityComparer.Default.Equals(propertySymbol.Type, expectedType))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.Shader.InvalidPropertyType,
                    propertyDeclaration.Identifier.GetLocation(),
                    propertySymbol.Name, attributeClass.Name, expectedType.ToDisplayString()));
                continue;
            }

            if (!byType.TryGetValue(containingType, out var list))
            {
                list = [];
                byType[containingType] = list;
            }

            list.Add((propertySymbol, attributeClass));
        }

        foreach (var (containingType, propertyList) in byType)
        {
            GenerateShaderProperties(context, containingType, propertyList, graphicsModuleType);
        }
    }

    /// <summary>
    /// Emits the implementing partial declaration for every generator-backed shader property on a type.
    /// </summary>
    private static void GenerateShaderProperties(SourceProductionContext context, INamedTypeSymbol containingType,
        List<(IPropertySymbol property, INamedTypeSymbol attributeClass)> properties,
        INamedTypeSymbol graphicsModuleType)
    {
        var namespaceName = containingType.ContainingNamespace.ToDisplayString();
        var typeName = containingType.Name;
        var kind = containingType.IsValueType ? "struct" : "class";
        var graphicsModuleDisplay = graphicsModuleType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var output = new SourceBuilder();
        output
            .Line("// <auto-generated/>")
            .Line()
            .Line($"namespace {namespaceName};")
            .Line($"partial {kind} {typeName}")
            .OpenBrace();

        foreach (var (property, attributeClass) in properties)
        {
            var attributeData = property.GetAttributes()
                .First(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attributeClass));
            var path = (string)attributeData.ConstructorArguments[0].Value!;
            var factoryMethod = attributeClass.ToDisplayString() == GraphicsShaderAttributeFullName
                ? "MakeGraphics"
                : "MakeCompute";
            var propertyType = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var pathLiteral = SymbolDisplay.FormatLiteral(path, quote: true);
            var accessibility = GetAccessibilityModifier(property.DeclaredAccessibility);

            output.Line(
                $"{accessibility} partial {propertyType} {property.Name} => field ??= {graphicsModuleDisplay}.Get().{factoryMethod}({pathLiteral});");
        }

        output.CloseBrace();

        context.AddSource($"Shader_{GeneratorUtils.GetSafeHintName(containingType)}.g.cs", output.ToSourceText());
    }

    private static string GetAccessibilityModifier(Accessibility accessibility) => accessibility switch
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
