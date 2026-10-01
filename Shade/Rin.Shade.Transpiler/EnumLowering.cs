using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Rin.Shade.Transpiler;

/// <summary>
/// Lowers a C# enum to a Slang enum with explicit values.
/// </summary>
internal static class EnumLowering
{
    public static string Lower(INamedTypeSymbol type, List<Diagnostic> diagnostics)
    {
        // Slang enums are always int-sized, so a different underlying type would silently disagree with the C# layout.
        if (type.EnumUnderlyingType?.SpecialType != SpecialType.System_Int32)
        {
            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedEnumUnderlyingType,
                type.Locations.FirstOrDefault() ?? Location.None, type.Name,
                type.EnumUnderlyingType?.Name ?? "unknown"));
            return "";
        }

        var writer = new SlangWriter();
        writer.OpenBrace($"enum {type.Name}");

        // Explicit values, so the result does not depend on C# and Slang auto-incrementing the same way.
        var members = type.GetMembers().OfType<IFieldSymbol>().Where(f => f.IsConst).ToArray();
        for (var i = 0; i < members.Length; i++)
        {
            var suffix = i == members.Length - 1 ? "" : ",";
            writer.Line($"{members[i].Name} = {members[i].ConstantValue}{suffix}");
        }

        writer.CloseBrace();
        return writer.ToString();
    }
}
