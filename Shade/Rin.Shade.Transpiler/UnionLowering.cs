using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Rin.Shade.Transpiler;

/// <summary>
/// Lowers an explicit-layout union to a header struct, one padded struct per variant path, and a
/// full-size "complete" struct.
/// </summary>
internal static class UnionLowering
{
    /// <summary>
    /// The generated struct name for a variant path below the root union.
    /// </summary>
    public static string PathName(ITypeSymbol root, IEnumerable<UnionVariant> path) =>
        $"{Naming.GeneratedPrefix}{root.Name}_{string.Join("_", path.Select(v => v.Member.Name))}";

    /// <summary>
    /// <see cref="PathName"/> qualified relative to the current scope.
    /// </summary>
    public static string QualifiedPathName(ITypeSymbol root, IEnumerable<UnionVariant> path) =>
        Naming.Qualify(root, PathName(root, path));

    /// <summary>
    /// The Slang text for the union, or an empty string after a diagnostic if its layout is invalid.
    /// </summary>
    public static string Lower(INamedTypeSymbol type, List<Diagnostic> diagnostics,
        IReadOnlyList<string>? nested = null)
    {
        var info = UnionLayout.Analyze(type, diagnostics);
        if (info is null) return "";

        var writer = new SlangWriter();
        writer.Append(StructLowering.Lower(type, diagnostics, info.Header, nested));

        foreach (var path in UnionLayout.AllPaths(info))
        {
            var first = path[0];
            // A nested union's payload is its header-only struct; its Complete struct would add padding twice.
            var payloadType = path.Count > 1
                ? QualifiedPathName(first.Member.Type, path.Skip(1))
                : UnionLayout.IsUnion(first.Member.Type)
                    ? Naming.Qualify(first.Member.Type, first.Member.Type.Name)
                    : TypeMapping.MapType(first.Member.Type);

            writer.Line();
            writer.OpenBrace($"struct {PathName(type, path)}");
            writer.Line($"{type.Name} header;");
            WritePadding(writer, (first.Offset - info.HeaderEnd) / 4, "_pad");
            writer.Line($"{payloadType} payload;");
            writer.CloseBrace();
        }

        writer.Line();
        writer.OpenBrace($"struct {info.CompleteName}");
        writer.Line($"{type.Name} header;");
        WritePadding(writer, (info.TotalSize - info.HeaderEnd) / 4, "_padding");
        writer.CloseBrace();

        return writer.ToString();
    }

    private static void WritePadding(SlangWriter writer, int words, string name)
    {
        if (words > 0) writer.Line($"uint {name}[{words}];");
    }
}
