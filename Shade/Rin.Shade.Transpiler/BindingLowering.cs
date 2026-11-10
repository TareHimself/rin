using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Operations;

namespace Rin.Shade.Transpiler;

/// <summary>
/// Lowers [BindingGroup] fields to module-scope Slang ParameterBlock&lt;T&gt; declarations. Unlike [Push]
/// there is no explicit binding: Slang assigns the set and binding, and the engine reads them back
/// through reflection.
/// </summary>
internal static class BindingLowering
{
    public static bool HasBindingGroupAttribute(IFieldSymbol field) =>
        field.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "Rin.Shade.BindingGroupAttribute");

    /// <summary>
    /// The name from the type's [BindlessBlock] attribute, or null if it has none.
    /// </summary>
    public static string? BindlessBlockName(ITypeSymbol type) =>
        type.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "Rin.Shade.BindlessBlockAttribute")
            ?.ConstructorArguments.FirstOrDefault().Value as string;

    /// <summary>
    /// Whether the field is one of a bindless block's resources, which live in the block's parameter block.
    /// </summary>
    public static bool IsBindlessBlockMember(IFieldSymbol field) =>
        field is { IsStatic: true, IsConst: false } && BindlessBlockName(field.ContainingType) is not null;

    /// <summary>
    /// The module-scope parameter block a bindless block's static fields are read through.
    /// </summary>
    public static string BindlessBlockGlobal(INamedTypeSymbol block) => $"rin_{Naming.ToSlangIdentifier(block.Name)}";

    /// <summary>
    /// The bindless blocks whose resources the bodies read, in first-use order.
    /// </summary>
    public static List<INamedTypeSymbol> FindBindlessBlocks(IEnumerable<IOperation> bodies) =>
        bodies.SelectMany(body => body.DescendantsAndSelf())
            .OfType<IFieldReferenceOperation>()
            .Where(reference => IsBindlessBlockMember(reference.Field))
            .Select(reference => reference.Field.ContainingType)
            .Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default)
            .ToList();

    /// <summary>
    /// The parameter block declaration for a bindless block.
    /// </summary>
    public static string LowerBindlessBlock(INamedTypeSymbol block) =>
        $"[BindlessBlock({SymbolDisplay.FormatLiteral(BindlessBlockName(block)!, quote: true)})] " +
        $"ParameterBlock<{TypeMapping.MapType(block)}> {BindlessBlockGlobal(block)};";

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

        return $"ParameterBlock<{TypeMapping.MapType(groupType)}> {Naming.ToSlangIdentifier(field.Name)};";
    }

    private static void Diagnose(List<Diagnostic> diagnostics, IFieldSymbol field, Location location, string reason) =>
        diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedBindingField, location, field.Name, reason));
}
