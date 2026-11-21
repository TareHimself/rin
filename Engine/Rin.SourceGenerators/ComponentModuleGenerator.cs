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
/// component and tag sorted by full type name, each saved component with its fields, their offsets and their
/// <c>FieldType</c>s.
/// </summary>
/// <remarks>
/// The module is <c>GeneratedComponents.Module</c> in a namespace named after the assembly, and an assembly attribute
/// derived from <c>ComponentModuleAttribute</c> exposes it to <c>ComponentModule.Of</c>. Offsets come from
/// <c>Unsafe.ByteOffset</c> between refs into a default instance, so they need no reflection under NativeAOT.
/// Generic, inaccessible and invalid structs are left out with a diagnostic, and so is any saved component with a field
/// the schema cannot describe (see <see cref="ComponentLayoutReader" />).
/// </remarks>
[Generator]
public class ComponentModuleGenerator : IIncrementalGenerator
{
    private const string ComponentDataName = "Rin.World.IComponentData";
    private const string TagDataName = "Rin.World.ITagData";
    private const string NotSavedName = "Rin.World.EcsNotSavedAttribute";
    private const string FieldType = "global::Rin.World.FieldType";
    private const string FieldKind = "global::Rin.World.FieldKind";
    private const string ComponentField = "global::Rin.World.ComponentField";
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
        var fullName = FullNameOf(type);
        var typeExpression = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var problem = ProblemOf(type, isTag);
        if (problem is not null || isTag || HasAttribute(type, NotSavedName))
            return new ComponentDeclaration(fullName, typeExpression, isTag, [], [], problem is null ? [] : [problem]);

        var layout = ComponentLayoutReader.Read(compilation, type);
        return new ComponentDeclaration(fullName, typeExpression, isTag, layout.Fields, layout.Types, layout.Problems);
    }

    private static bool HasAttribute(INamedTypeSymbol type, string attributeName) =>
        type.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() == attributeName);

    private static bool Implements(INamedTypeSymbol type, INamedTypeSymbol @interface) =>
        type.AllInterfaces.Contains(@interface, SymbolEqualityComparer.Default);

    private static string FullNameOf(INamedTypeSymbol type)
    {
        var name = type.MetadataName;
        for (var outer = type.ContainingType; outer is not null; outer = outer.ContainingType)
            name = outer.MetadataName + "+" + name;
        return type.ContainingNamespace.IsGlobalNamespace ? name : type.ContainingNamespace.ToDisplayString() + "." + name;
    }

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
        foreach (var problem in declaration.Problems)
            context.ReportDiagnostic(problem);

        var components = unique
            .Where(static declaration => declaration.Problems.IsEmpty)
            .OrderBy(static declaration => declaration.FullName, StringComparer.Ordinal)
            .ToList();
        if (components.Count == 0) return;

        var fieldTypes = components
            .SelectMany(static component => component.FieldTypes)
            .GroupBy(static type => type.TypeExpression)
            .Select(static group => group.First())
            .ToList();
        context.AddSource("ComponentModule.g.cs", ModuleSource(NamespaceOf(assemblyName), components, fieldTypes).ToSourceText());
    }

    private static SourceBuilder ModuleSource(string namespaceName, List<ComponentDeclaration> components,
        List<FieldTypeDeclaration> fieldTypes)
    {
        var typeNames = new Dictionary<string, string>();
        for (var i = 0; i < fieldTypes.Count; i++) typeNames[fieldTypes[i].TypeExpression] = $"FieldType{i}";

        var output = new SourceBuilder()
            .Line("// <auto-generated/>")
            .Line($"[assembly: global::{namespaceName}.GeneratedComponentModuleAttribute]")
            .Line()
            .Line($"namespace {namespaceName};")
            .Line()
            .Line("internal static class GeneratedComponents")
            .OpenBrace();

        // Static initializers run in textual order, so every field type is built before the module that uses it.
        for (var i = 0; i < fieldTypes.Count; i++)
            output.Line($"private static readonly {FieldType} FieldType{i} = {FieldTypeValue(fieldTypes[i], i)};");

        if (fieldTypes.Count > 0) output.Line();
        output.Line($"public static {ComponentModule} Module {{ get; }} = new(").Indent();
        for (var i = 0; i < components.Count; i++)
            output.Line($"Component{i}(){(i < components.Count - 1 ? "," : ");")}");
        output.Outdent();

        for (var i = 0; i < components.Count; i++) WriteComponentMethod(output, i, components[i], typeNames);

        for (var i = 0; i < fieldTypes.Count; i++)
        {
            if (fieldTypes[i].Kind != "Struct") continue;
            output.Line();
            WriteFieldsMethod(output, $"FieldsOfType{i}", fieldTypes[i].TypeExpression, fieldTypes[i].Fields, typeNames);
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

    private static string FieldTypeValue(FieldTypeDeclaration type, int index) =>
        type.Kind == "Struct"
            ? $"{FieldType}.Struct<{type.TypeExpression}>(FieldsOfType{index}())"
            : $"{FieldType}.Of<{type.TypeExpression}>({FieldKind}.{type.Kind})";

    private static void WriteComponentMethod(SourceBuilder output, int index, ComponentDeclaration component,
        Dictionary<string, string> typeNames)
    {
        output.Line();
        if (component.Fields.IsEmpty)
        {
            output.Line($"private static {ComponentInfo} Component{index}() => {ComponentInfo}.Of<{component.TypeExpression}>();");
            return;
        }

        output
            .Line($"private static {ComponentInfo} Component{index}() => {ComponentInfo}.Of<{component.TypeExpression}>(FieldsOfComponent{index}());")
            .Line();
        WriteFieldsMethod(output, $"FieldsOfComponent{index}", component.TypeExpression, component.Fields, typeNames);
    }

    private static void WriteFieldsMethod(SourceBuilder output, string methodName, string ownerType,
        ImmutableArray<FieldDeclaration> fields, Dictionary<string, string> typeNames)
    {
        var hidden = new List<(string Accessor, FieldDeclaration Field)>();
        output
            .Line($"private static {ComponentField}[] {methodName}()")
            .OpenBrace()
            .Line($"var probe = default({ownerType});")
            .Line($"ref var start = ref {Unsafe}.As<{ownerType}, byte>(ref probe);")
            .Line("return [")
            .Indent();
        foreach (var field in fields)
        {
            var target = field.IsVisible ? $"{Unsafe}.AsRef(in probe.{field.Identifier})" : $"{methodName}_{field.Name}(ref probe)";
            if (!field.IsVisible) hidden.Add(($"{methodName}_{field.Name}", field));
            var fieldRef = $"ref {Unsafe}.As<{field.TypeExpression}, byte>(ref {target})";
            var isProcessLocal = field.IsProcessLocal ? "true" : "false";
            output.Line($"new(\"{field.Name}\", {typeNames[field.TypeExpression]}, OffsetOf(ref start, {fieldRef}), {isProcessLocal}),");
        }

        output.Outdent().Line("];").CloseBrace();

        foreach (var (accessor, field) in hidden)
            output
                .Line()
                .Line($"[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Field, Name = \"{field.Identifier}\")]")
                .Line($"private static extern ref {field.TypeExpression} {accessor}(ref {ownerType} owner);");
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
