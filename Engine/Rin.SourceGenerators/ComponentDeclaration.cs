using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Rin.SourceGenerators;

/// <summary>
/// One <c>IComponentData</c> struct as <see cref="ComponentModuleGenerator" /> sees it.
/// </summary>
/// <param name="FullName">The name <c>Type.FullName</c> gives at run time, which orders the module.</param>
/// <param name="FieldTypes">Every type its fields use, each after the types its own fields use.</param>
/// <param name="Requires">The type expressions of its <c>EcsRequiresAttribute&lt;T&gt;</c> arguments.</param>
/// <param name="Problems">Why the struct is left out of the module, or empty if it is in.</param>
internal sealed record ComponentDeclaration(
    string FullName,
    string TypeExpression,
    bool IsTag,
    ImmutableArray<FieldDeclaration> Fields,
    ImmutableArray<FieldTypeDeclaration> FieldTypes,
    ImmutableArray<string> Requires,
    ImmutableArray<Diagnostic> Problems);

/// <summary>
/// An instance field of a component or of a struct one of its fields uses.
/// </summary>
/// <param name="Name">The field's name, or its property's for an auto-property.</param>
/// <param name="Identifier">The name as C# source, escaped with <c>@</c> when it is a keyword.</param>
/// <param name="IsVisible">False for a field the generated module reaches through <c>UnsafeAccessor</c>, by its
/// metadata <paramref name="Identifier" />.</param>
internal sealed record FieldDeclaration(string Name, string Identifier, string TypeExpression, bool IsProcessLocal,
    bool IsDerived, bool IsVisible);

/// <summary>
/// A type used by a component field, with the <c>Rin.World.FieldKind</c> member that describes it.
/// </summary>
/// <param name="Fields">The fields of a <c>Struct</c>, empty for every other kind.</param>
internal sealed record FieldTypeDeclaration(string TypeExpression, string Kind, ImmutableArray<FieldDeclaration> Fields);
