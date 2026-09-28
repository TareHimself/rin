using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Rin.Shade.Transpiler;

internal static class EnumLowering
{
    public static string Lower(INamedTypeSymbol type, List<Diagnostic> diagnostics)
    {
        // A non-default underlying type (byte, long, ...) changes the enum's real size - silently
        // ignoring it would let the emitted Slang enum (always int-sized) disagree with what C#
        // actually lays out, the exact silent-corruption shape this whole project exists to prevent.
        if (type.EnumUnderlyingType?.SpecialType != SpecialType.System_Int32)
        {
            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedEnumUnderlyingType,
                type.Locations.FirstOrDefault() ?? Location.None, type.Name,
                type.EnumUnderlyingType?.Name ?? "unknown"));
            return "";
        }

        var writer = new SlangWriter();
        writer.OpenBrace($"enum {type.Name}");

        // Explicit values always, rather than relying on declaration-order auto-increment matching
        // between C# and Slang - the two only ever agree by accident otherwise.
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
