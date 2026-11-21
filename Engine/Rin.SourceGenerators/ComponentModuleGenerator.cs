using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Rin.SourceGenerators;

/// <summary>
/// Emits one <c>Rin.World.ComponentModule</c> per assembly that declares <c>IComponentData</c> structs: every
/// component and tag sorted by full type name, each with its public instance fields and their offsets.
/// </summary>
/// <remarks>
/// The module is <c>GeneratedComponents.Module</c> in a namespace named after the assembly, and an assembly attribute
/// derived from <c>ComponentModuleAttribute</c> exposes it to <c>ComponentModule.Of</c>. Offsets come from
/// <c>Unsafe.ByteOffset</c> between refs into a default instance, so they need no reflection under NativeAOT.
/// Generic, inaccessible and invalid structs are left out with a diagnostic.
/// </remarks>
[Generator]
public class ComponentModuleGenerator : IIncrementalGenerator
{
    private const string ComponentDataName = "Rin.World.IComponentData";
    private const string TagDataName = "Rin.World.ITagData";
    private const string ComponentInfo = "global::Rin.World.ComponentInfo";
    private const string ComponentModule = "global::Rin.World.ComponentModule";
    private const string Unsafe = "global::System.Runtime.CompilerServices.Unsafe";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var components = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsStructWithBaseList(node),
                transform: static (syntax, cancellationToken) => ReadComponent(syntax, cancellationToken))
            .Where(static component => component is not null)
            .Select(static (component, _) => component!);

        var assemblyName = context.CompilationProvider
            .Select(static (compilation, _) => compilation.AssemblyName ?? "Components");

        context.RegisterSourceOutput(components.Collect().Combine(assemblyName),
            static (spc, source) => Execute(spc, source.Left, source.Right));
    }

    private static bool IsStructWithBaseList(SyntaxNode node) => node switch
    {
        StructDeclarationSyntax { BaseList: not null } => true,
        RecordDeclarationSyntax { BaseList: not null } record => record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword),
        _ => false
    };

    private static ComponentDeclaration? ReadComponent(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.SemanticModel.GetDeclaredSymbol(context.Node, cancellationToken) is not INamedTypeSymbol type)
            return null;

        var compilation = context.SemanticModel.Compilation;
        var componentData = compilation.GetTypeByMetadataName(ComponentDataName);
        if (componentData is null || !Implements(type, componentData)) return null;

        var tagData = compilation.GetTypeByMetadataName(TagDataName);
        var isTag = tagData is not null && Implements(type, tagData);
        return new ComponentDeclaration(
            FullNameOf(type),
            type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            isTag,
            isTag ? [] : PublicFieldsOf(type),
            ProblemOf(type, isTag));
    }

    private static bool Implements(INamedTypeSymbol type, INamedTypeSymbol @interface) =>
        type.AllInterfaces.Contains(@interface, SymbolEqualityComparer.Default);

    private static string FullNameOf(INamedTypeSymbol type)
    {
        var name = type.MetadataName;
        for (var outer = type.ContainingType; outer is not null; outer = outer.ContainingType)
            name = outer.MetadataName + "+" + name;
        return type.ContainingNamespace.IsGlobalNamespace ? name : type.ContainingNamespace.ToDisplayString() + "." + name;
    }

    private static ImmutableArray<ComponentFieldDeclaration> PublicFieldsOf(INamedTypeSymbol type) =>
        type.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(static field => !field.IsStatic && field.DeclaredAccessibility == Accessibility.Public)
            .Where(static field => !field.IsFixedSizeBuffer && field.Type is not (IPointerTypeSymbol or IFunctionPointerTypeSymbol))
            .Select(static field => new ComponentFieldDeclaration(
                field.Name,
                EscapeKeyword(field.Name),
                field.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))
            .ToImmutableArray();

    private static Diagnostic? ProblemOf(INamedTypeSymbol type, bool isTag)
    {
        var descriptor = DescriptorOfProblem(type, isTag);
        return descriptor is null
            ? null
            : Diagnostic.Create(descriptor, type.Locations.FirstOrDefault(), type.ToDisplayString());
    }

    private static DiagnosticDescriptor? DescriptorOfProblem(INamedTypeSymbol type, bool isTag)
    {
        if (IsGeneric(type)) return Diagnostics.Components.GenericComponent;
        if (!IsVisibleToItsAssembly(type)) return Diagnostics.Components.InaccessibleComponent;
        if (!type.IsUnmanagedType) return Diagnostics.Components.ComponentMustBeUnmanaged;
        if (isTag && type.GetMembers().OfType<IFieldSymbol>().Any(static field => !field.IsStatic))
            return Diagnostics.Components.TagMustNotHaveFields;
        return null;
    }

    private static bool IsGeneric(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.ContainingType)
            if (current.TypeParameters.Length > 0) return true;
        return false;
    }

    private static bool IsVisibleToItsAssembly(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.ContainingType)
        {
            if (current.IsFileLocal) return false;
            if (current.DeclaredAccessibility is Accessibility.Private or Accessibility.Protected
                or Accessibility.ProtectedAndInternal) return false;
        }

        return true;
    }

    private static void Execute(SourceProductionContext context, ImmutableArray<ComponentDeclaration> declarations,
        string assemblyName)
    {
        var unique = declarations.GroupBy(static declaration => declaration.FullName).Select(static group => group.First()).ToList();
        foreach (var declaration in unique)
            if (declaration.Problem is not null) context.ReportDiagnostic(declaration.Problem);

        var components = unique
            .Where(static declaration => declaration.Problem is null)
            .OrderBy(static declaration => declaration.FullName, StringComparer.Ordinal)
            .ToList();
        if (components.Count == 0) return;

        context.AddSource("ComponentModule.g.cs", ModuleSource(NamespaceOf(assemblyName), components).ToSourceText());
    }

    private static SourceBuilder ModuleSource(string namespaceName, List<ComponentDeclaration> components)
    {
        var output = new SourceBuilder()
            .Line("// <auto-generated/>")
            .Line($"[assembly: global::{namespaceName}.GeneratedComponentModuleAttribute]")
            .Line()
            .Line($"namespace {namespaceName};")
            .Line()
            .Line("internal static class GeneratedComponents")
            .OpenBrace()
            .Line($"public static {ComponentModule} Module {{ get; }} = new(")
            .Indent();

        for (var i = 0; i < components.Count; i++)
            output.Line($"Component{i}(){(i < components.Count - 1 ? "," : ");")}");
        output.Outdent();

        for (var i = 0; i < components.Count; i++)
        {
            output.Line();
            WriteComponentMethod(output, i, components[i]);
        }

        output
            .Line()
            .Line($"private static int OffsetOf(ref byte start, ref byte field) => (int){Unsafe}.ByteOffset(ref start, ref field);")
            .CloseBrace()
            .Line()
            .Line($"internal sealed class GeneratedComponentModuleAttribute : global::Rin.World.ComponentModuleAttribute")
            .OpenBrace()
            .Line($"public override {ComponentModule} Module => GeneratedComponents.Module;")
            .CloseBrace();
        return output;
    }

    private static void WriteComponentMethod(SourceBuilder output, int index, ComponentDeclaration component)
    {
        var type = component.TypeExpression;
        output.Line($"private static {ComponentInfo} Component{index}()").OpenBrace();
        if (component.Fields.IsEmpty)
        {
            output.Line($"return {ComponentInfo}.Of<{type}>();").CloseBrace();
            return;
        }

        output
            .Line($"var probe = default({type});")
            .Line($"ref var start = ref {Unsafe}.As<{type}, byte>(ref probe);")
            .Line($"return {ComponentInfo}.Of<{type}>([")
            .Indent();
        foreach (var field in component.Fields)
        {
            var fieldRef = $"ref {Unsafe}.As<{field.TypeExpression}, byte>(ref {Unsafe}.AsRef(in probe.{field.Identifier}))";
            output.Line($"new(\"{field.Name}\", typeof({field.TypeExpression}), OffsetOf(ref start, {fieldRef})),");
        }

        output.Outdent().Line("]);").CloseBrace();
    }

    private static string NamespaceOf(string assemblyName) =>
        string.Join(".", assemblyName.Split('.').Select(IdentifierOf));

    private static string IdentifierOf(string segment)
    {
        var identifier = new string(segment.Select(static c => char.IsLetterOrDigit(c) || c == '_' ? c : '_').ToArray());
        if (identifier.Length == 0 || char.IsDigit(identifier[0])) identifier = "_" + identifier;
        return EscapeKeyword(identifier);
    }

    private static string EscapeKeyword(string name) =>
        SyntaxFacts.GetKeywordKind(name) == SyntaxKind.None ? name : "@" + name;
}
