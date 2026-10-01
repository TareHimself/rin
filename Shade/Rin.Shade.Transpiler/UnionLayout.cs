using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Rin.Shade.Transpiler;

/// <summary>
/// One overlapping member of a union, with its byte offset and size.
/// </summary>
internal sealed record UnionVariant(StructMember Member, int Offset, int Size);

/// <summary>
/// The analyzed layout of a union: the shared header members, the overlapping variants, and the header
/// size, total size and alignment in bytes.
/// </summary>
internal sealed record UnionInfo(
    INamedTypeSymbol Type,
    List<StructMember> Header,
    List<UnionVariant> Variants,
    int HeaderEnd,
    int TotalSize,
    int Align)
{
    public string VariantName(UnionVariant variant) => $"{Type.Name}_{variant.Member.Name}";
    public string CompleteName => $"{Naming.GeneratedPrefix}Complete{Type.Name}";
}

/// <summary>
/// Reads an explicit-layout struct's [FieldOffset] layout: members whose byte ranges overlap are the
/// variants, everything else is the shared header. Explicit layout is what marks a struct as a union.
/// Each union has a single overlap region at its tail; a variant's payload may itself be a union.
/// </summary>
internal static class UnionLayout
{
    private readonly record struct Placed(StructMember Member, int Offset, int Size, int Align);

    private const int ExplicitLayoutKind = 2;

    /// <summary>
    /// Whether the type is a struct with explicit layout.
    /// </summary>
    public static bool IsUnion(ITypeSymbol type) =>
        type.TypeKind == TypeKind.Struct &&
        type.GetAttributes().Any(a =>
            a.AttributeClass?.ToDisplayString() == "System.Runtime.InteropServices.StructLayoutAttribute" &&
            a.ConstructorArguments.FirstOrDefault().Value is ExplicitLayoutKind);

    /// <summary>
    /// Whether the field is one of its union's overlapping variants, as opposed to a header member.
    /// </summary>
    public static bool IsVariant(IFieldSymbol field)
    {
        if (!IsUnion(field.ContainingType)) return false;
        var info = Analyze(field.ContainingType, []);
        return info is not null && info.Variants.Any(v => SymbolEqualityComparer.Default.Equals(v.Member.Field, field));
    }

    /// <summary>
    /// Splits the union into header and variants and validates the layout. Reports a diagnostic and returns null if invalid.
    /// </summary>
    public static UnionInfo? Analyze(INamedTypeSymbol type, List<Diagnostic> diagnostics)
    {
        var location = type.Locations.FirstOrDefault() ?? Location.None;

        UnionInfo? Fail(string reason)
        {
            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.InvalidUnionLayout, location, type.Name, reason));
            return null;
        }

        var placed = new List<Placed>();
        foreach (var member in StructMembers.Instance(type))
        {
            var offsetAttribute = member.Field.GetAttributes().FirstOrDefault(a =>
                a.AttributeClass?.ToDisplayString() == "System.Runtime.InteropServices.FieldOffsetAttribute");
            if (offsetAttribute?.ConstructorArguments.FirstOrDefault().Value is not int offset)
                return Fail($"'{member.Name}' has no [FieldOffset]");

            if (!ShaderLayout.TrySizeAndAlign(member.Type, out var size, out var align))
                return Fail($"the size of '{member.Name}' ({member.Type.ToDisplayString()}) can't be computed");

            placed.Add(new Placed(member, offset, size, align));
        }

        bool Overlaps(Placed a, Placed b) => a.Offset < b.Offset + b.Size && b.Offset < a.Offset + a.Size;

        var variants = placed.Where(a => placed.Any(b => a != b && Overlaps(a, b))).OrderBy(v => v.Offset).ToList();
        var header = placed.Except(variants).OrderBy(h => h.Offset).ToList();

        if (variants.Count == 0) return Fail("no fields overlap, so there are no variants");

        var regionStart = variants[0].Offset;
        var regionEnd = variants[0].Offset + variants[0].Size;
        foreach (var variant in variants.Skip(1))
        {
            if (variant.Offset >= regionEnd)
                return Fail($"'{variant.Member.Name}' starts a second overlap region - only one union region per union is supported");

            regionEnd = System.Math.Max(regionEnd, variant.Offset + variant.Size);
        }

        var headerEnd = 0;
        foreach (var member in header)
        {
            var expected = ShaderLayout.AlignUp(headerEnd, member.Align);
            if (member.Offset != expected)
                return Fail($"header field '{member.Member.Name}' is at offset {member.Offset}, but sequential layout puts it at {expected}");

            headerEnd = member.Offset + member.Size;
        }

        if (headerEnd > regionStart)
            return Fail("a header field sits after the start of the union region - the union must be at the tail");

        foreach (var variant in variants)
            if ((variant.Offset - headerEnd) % 4 != 0)
                return Fail($"the gap before '{variant.Member.Name}' isn't a multiple of 4 bytes");

        var unionAlign = placed.Max(p => p.Align);
        return new UnionInfo(type,
            header.Select(h => h.Member).ToList(),
            variants.Select(v => new UnionVariant(v.Member, v.Offset, v.Size)).ToList(),
            headerEnd, ShaderLayout.AlignUp(regionEnd, unionAlign), unionAlign);
    }

    /// <summary>
    /// Every variant path below a union, depth first, internal nodes included: a variant whose
    /// payload is itself a union yields its own path (Foo_Bar) and then one per nested variant
    /// (Foo_Bar_Car), so any chain of field accesses names a struct that exists.
    /// </summary>
    public static IEnumerable<List<UnionVariant>> AllPaths(UnionInfo info)
    {
        foreach (var variant in info.Variants)
        {
            yield return [variant];

            if (variant.Member.Type is INamedTypeSymbol nested && IsUnion(nested) &&
                Analyze(nested, []) is { } nestedInfo)
                foreach (var tail in AllPaths(nestedInfo))
                    yield return [variant, ..tail];
        }
    }
}
