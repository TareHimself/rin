using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Rin.Shade.Transpiler;

/// <summary>
/// Lowers [GroupShared] fields to module-scope Slang <c>groupshared</c> declarations, referenced by bare name
/// from the shader's bodies like a binding group.
/// </summary>
internal static class GroupSharedLowering
{
    public static bool HasGroupSharedAttribute(IFieldSymbol field) =>
        field.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "Rin.Shade.GroupSharedAttribute");

    /// <summary>
    /// The Slang declaration for the field, or null after reporting a diagnostic if it is not a static field of a legal shader type.
    /// </summary>
    public static string? Lower(IFieldSymbol field, List<Diagnostic> diagnostics)
    {
        var location = field.Locations.FirstOrDefault() ?? Location.None;

        if (!field.IsStatic)
        {
            Diagnose(diagnostics, field, location, "[GroupShared] fields must be static");
            return null;
        }

        if (field.Type is IArrayTypeSymbol)
        {
            Diagnose(diagnostics, field, location, "a [GroupShared] array must be an [InlineArray] struct, so its length is known");
            return null;
        }

        if (!TypeMapping.IsLegalShaderType(field.Type) || field.Type is INamedTypeSymbol { Name: "BufferRef" })
        {
            Diagnose(diagnostics, field, location, $"'{field.Type.ToDisplayString()}' cannot live in group-shared memory");
            return null;
        }

        var name = Naming.ToSlangIdentifier(field.Name);
        var length = InlineArrays.TryGet(field.Type, out _, out var count) ? $"[{count}]" : "";
        return $"groupshared {TypeMapping.MapType(field.Type)} {name}{length};";
    }

    private static void Diagnose(List<Diagnostic> diagnostics, IFieldSymbol field, Location location, string reason) =>
        diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedGroupSharedField, location, field.Name, reason));
}
