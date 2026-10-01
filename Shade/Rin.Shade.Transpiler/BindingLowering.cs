using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Rin.Shade.Transpiler;

/// <summary>
/// Lowers [BindingGroup] fields to module-scope Slang ParameterBlock&lt;T&gt; declarations. Unlike [Push]
/// there is no explicit binding: Slang assigns the set and binding, and the engine reads them back
/// through reflection.
/// </summary>
internal static class BindingLowering
{
    /// <summary>
    /// A field is a parameter block if it says so, or if its type is a named bindless block.
    /// </summary>
    public static bool HasBindingGroupAttribute(IFieldSymbol field) =>
        field.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "Rin.Shade.BindingGroupAttribute") ||
        BindlessBlockName(field) is not null;

    /// <summary>
    /// The name from the field type's [BindlessBlock] attribute, or null if the type has none.
    /// </summary>
    public static string? BindlessBlockName(IFieldSymbol field) =>
        field.Type.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "Rin.Shade.BindlessBlockAttribute")
            ?.ConstructorArguments.FirstOrDefault().Value as string;

    public const string BindlessAttributeDeclaration = """
        [__AttributeUsage(_AttributeTargets.Var)]
        struct BindlessBlockAttribute { string name; };

        """;

    /// <summary>
    /// The Slang declaration for the field, or null after reporting a diagnostic if it is not a static field of a plain struct type.
    /// </summary>
    public static string? Lower(IFieldSymbol field, List<Diagnostic> diagnostics)
    {
        var location = field.Locations.FirstOrDefault() ?? Location.None;

        if (!field.IsStatic)
        {
            Diagnose(diagnostics, field, location, "[BindingGroup] fields must be static");
            return null;
        }

        // Every C# value type is a struct (float, Vector2, ...), so IsBuiltIn is needed to leave only user structs.
        if (field.Type is not INamedTypeSymbol { TypeKind: TypeKind.Struct } groupType ||
            TypeMapping.IsBuiltIn(groupType))
        {
            Diagnose(diagnostics, field, location,
                $"'{field.Type.ToDisplayString()}' is not a struct - a binding group's fields must be " +
                "declared on a dedicated struct type, the same way a [Push] field's type works");
            return null;
        }

        var name = Naming.ToSlangIdentifier(field.Name);
        var declaration = $"ParameterBlock<{TypeMapping.MapType(groupType)}> {name};";
        return BindlessBlockName(field) is { } blockName
            ? $"[BindlessBlock({SymbolDisplay.FormatLiteral(blockName, quote: true)})] {declaration}"
            : declaration;
    }

    private static void Diagnose(List<Diagnostic> diagnostics, IFieldSymbol field, Location location, string reason) =>
        diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedBindingField, location, field.Name, reason));
}
