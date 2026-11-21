using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Rin.SourceGenerators;

/// <summary>
/// One <c>IComponentData</c> struct as <see cref="ComponentModuleGenerator" /> sees it.
/// </summary>
/// <param name="FullName">The name <c>Type.FullName</c> gives at run time, which orders the module.</param>
/// <param name="Problem">Why the struct is left out of the module, or null if it is in.</param>
internal sealed record ComponentDeclaration(
    string FullName,
    string TypeExpression,
    bool IsTag,
    ImmutableArray<ComponentFieldDeclaration> Fields,
    Diagnostic? Problem);

/// <summary>
/// A public instance field of a component.
/// </summary>
/// <param name="Identifier">The name as C# source, escaped with <c>@</c> when it is a keyword.</param>
internal sealed record ComponentFieldDeclaration(string Name, string Identifier, string TypeExpression);
