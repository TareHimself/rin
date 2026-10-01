using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Rin.Shade.Transpiler;

internal static class UnionLowering
{
    public static string PathName(ITypeSymbol root, IEnumerable<UnionVariant> path) =>
        $"{Naming.GeneratedPrefix}{root.Name}_{string.Join("_", path.Select(v => v.Member.Name))}";

    public static string QualifiedPathName(ITypeSymbol root, IEnumerable<UnionVariant> path) =>
        Naming.Qualify(root, PathName(root, path));

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
            // A nested union's own payload is its header-only struct: the variant struct already
            // names the rest of it by path, and a Complete struct here would add padding twice.
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
