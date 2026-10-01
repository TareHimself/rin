using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Rin.Shade.Transpiler;

// [BindingGroup] fields lower to a Slang ParameterBlock<T> - module-scope, like [Push], but with no
// explicit binding: Slang assigns the whole block's set/binding on its own, and the engine reads
// back wherever it landed via reflection, the same way an ordinary shader parameter already works.
// The group's own struct type needs no special marker - it's walked structurally by
// TypeGraph/StructLowering exactly like a [Push] field's struct type already is.
internal static class BindingLowering
{
    /// <summary>A field is a parameter block if it says so, or if its type is a named bindless block.</summary>
    public static bool HasBindingGroupAttribute(IFieldSymbol field) =>
        field.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "Rin.Shade.BindingGroupAttribute") ||
        BindlessBlockName(field) is not null;

    public static string? BindlessBlockName(IFieldSymbol field) =>
        field.Type.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "Rin.Shade.BindlessBlockAttribute")
            ?.ConstructorArguments.FirstOrDefault().Value as string;

    public const string BindlessAttributeDeclaration = """
        [__AttributeUsage(_AttributeTargets.Var)]
        struct BindlessBlockAttribute { string name; };

        """;

    public static string? Lower(IFieldSymbol field, List<Diagnostic> diagnostics)
    {
        var location = field.Locations.FirstOrDefault() ?? Location.None;

        if (!field.IsStatic)
        {
            Diagnose(diagnostics, field, location, "[BindingGroup] fields must be static");
            return null;
        }

        // TypeKind.Struct alone isn't enough - float/int/Vector2/Texture2D etc. are all structs too
        // (every C# value type is), so IsBuiltIn excludes those, leaving only plain user structs.
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
