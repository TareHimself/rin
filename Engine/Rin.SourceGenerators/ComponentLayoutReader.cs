using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Rin.SourceGenerators;

/// <summary>
/// Reads the fields of a saved component and of every struct they use, giving each type a <c>FieldKind</c> and
/// reporting any field the component schema cannot describe.
/// </summary>
internal sealed class ComponentLayoutReader
{
    private const string ProcessLocalName = "Rin.World.EcsProcessLocalAttribute";
    private const string DerivedName = "Rin.World.EcsDerivedAttribute";
    private const string EntityName = "Rin.World.Entity";

    private static readonly Dictionary<string, string> NamedKinds = new()
    {
        ["System.Numerics.Vector2"] = "Vector2",
        ["System.Numerics.Vector3"] = "Vector3",
        ["System.Numerics.Vector4"] = "Vector4",
        ["System.Numerics.Quaternion"] = "Quaternion",
        ["System.Numerics.Matrix4x4"] = "Matrix4x4",
        [EntityName] = "Entity"
    };

    private readonly Compilation _compilation;
    private readonly INamedTypeSymbol _component;
    private readonly List<FieldTypeDeclaration> _types = [];
    private readonly HashSet<string> _seenTypes = [];
    private readonly List<Diagnostic> _problems = [];

    private ComponentLayoutReader(Compilation compilation, INamedTypeSymbol component)
    {
        _compilation = compilation;
        _component = component;
    }

    public ImmutableArray<FieldDeclaration> Fields { get; private set; }

    public ImmutableArray<FieldTypeDeclaration> Types => _types.ToImmutableArray();

    public ImmutableArray<Diagnostic> Problems => _problems.ToImmutableArray();

    public static ComponentLayoutReader Read(Compilation compilation, INamedTypeSymbol component)
    {
        var reader = new ComponentLayoutReader(compilation, component);
        reader.Fields = reader.FieldsOf(component, "", null);
        return reader;
    }

    private ImmutableArray<FieldDeclaration> FieldsOf(INamedTypeSymbol owner, string path, Location? nestedLocation)
    {
        var fields = ImmutableArray.CreateBuilder<FieldDeclaration>();
        foreach (var field in owner.GetMembers().OfType<IFieldSymbol>())
        {
            if (field.IsStatic) continue;

            var name = field.AssociatedSymbol?.Name ?? field.Name;
            var fieldPath = path.Length == 0 ? name : path + "." + name;
            var location = nestedLocation ?? (field.AssociatedSymbol ?? field).Locations.FirstOrDefault();
            if (field.IsFixedSizeBuffer)
            {
                Report(fieldPath, location, "it is a fixed-size buffer");
                continue;
            }

            var isProcessLocal = HasAttribute(field, ProcessLocalName);
            var typeExpression = TypeOf(field.Type, isProcessLocal, fieldPath, location);
            if (typeExpression is null) continue;

            var isVisible = _compilation.IsSymbolAccessibleWithin(field, _compilation.Assembly);
            var identifier = isVisible ? EscapeKeyword(field.Name) : field.Name;
            fields.Add(new FieldDeclaration(name, identifier, typeExpression, isProcessLocal, HasAttribute(field, DerivedName),
                isVisible));
        }

        return fields.ToImmutable();
    }

    private static bool HasAttribute(IFieldSymbol field, string attributeName) =>
        field.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() == attributeName);

    private string? TypeOf(ITypeSymbol type, bool isProcessLocal, string path, Location? location)
    {
        var expression = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (type is IPointerTypeSymbol or IFunctionPointerTypeSymbol)
            return Report(path, location, "it is a pointer");

        var kind = KindOf(type);
        if (kind is not null) return AddType(expression, kind, ImmutableArray<FieldDeclaration>.Empty);
        if (isProcessLocal) return AddType(expression, "Opaque", ImmutableArray<FieldDeclaration>.Empty);
        if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            return Report(path, location, "it is a Nullable<T>");
        if (type is not INamedTypeSymbol { TypeKind: TypeKind.Struct } named || type.SpecialType != SpecialType.None)
            return Report(path, location, $"its type '{type.ToDisplayString()}' has no field kind");
        if (!_compilation.IsSymbolAccessibleWithin(named, _compilation.Assembly))
            return Report(path, location, $"its type '{type.ToDisplayString()}' is not visible to the rest of its assembly");

        var fields = FieldsOf(named, path, location);
        return AddType(expression, "Struct", fields);
    }

    private static string? KindOf(ITypeSymbol type)
    {
        if (type.TypeKind == TypeKind.Enum) return "Enum";
        var special = type.SpecialType switch
        {
            SpecialType.System_Boolean => "Bool",
            SpecialType.System_SByte => "Int8",
            SpecialType.System_Byte => "UInt8",
            SpecialType.System_Int16 => "Int16",
            SpecialType.System_UInt16 => "UInt16",
            SpecialType.System_Int32 => "Int32",
            SpecialType.System_UInt32 => "UInt32",
            SpecialType.System_Int64 => "Int64",
            SpecialType.System_UInt64 => "UInt64",
            SpecialType.System_Single => "Float32",
            SpecialType.System_Double => "Float64",
            _ => null
        };
        return special ?? (NamedKinds.TryGetValue(type.ToDisplayString(), out var named) ? named : null);
    }

    private string AddType(string expression, string kind, ImmutableArray<FieldDeclaration> fields)
    {
        if (_seenTypes.Add(expression)) _types.Add(new FieldTypeDeclaration(expression, kind, fields));
        return expression;
    }

    private string? Report(string path, Location? location, string reason)
    {
        _problems.Add(Diagnostic.Create(Diagnostics.Components.UnknownFieldType, location, path, _component.ToDisplayString(), reason));
        return null;
    }

    private static string EscapeKeyword(string name) =>
        SyntaxFacts.GetKeywordKind(name) == SyntaxKind.None ? name : "@" + name;
}
