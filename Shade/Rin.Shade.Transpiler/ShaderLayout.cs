using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Rin.Shade.Transpiler;

/// <summary>
/// Size and alignment of a type under Slang's scalar layout, which is also what C# sequential layout
/// produces for the same blittable types - the two agree by construction here, which is what lets a
/// union's Slang padding be derived from its C# offsets.
/// </summary>
internal static class ShaderLayout
{
    public static bool TrySizeAndAlign(ITypeSymbol type, out int size, out int align)
    {
        size = 0;
        align = 4;

        switch (type.SpecialType)
        {
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Single:
                size = 4;
                return true;
        }

        if (type is INamedTypeSymbol { TypeKind: TypeKind.Enum, EnumUnderlyingType.SpecialType: SpecialType.System_Int32 })
        {
            size = 4;
            return true;
        }

        switch (type.OriginalDefinition.ToDisplayString())
        {
            case "System.Numerics.Vector2": size = 8; return true;
            case "System.Numerics.Vector3": size = 12; return true;
            case "System.Numerics.Vector4": size = 16; return true;
            case "System.Numerics.Matrix4x4": size = 64; return true;
        }

        if (type is INamedTypeSymbol { Name: "BufferRef", TypeArguments.Length: 1 })
        {
            size = 8;
            align = 8;
            return true;
        }

        if (InlineArrays.TryGet(type, out var element, out var length))
        {
            if (!TrySizeAndAlign(element, out var elementSize, out align)) return false;
            size = elementSize * length;
            return true;
        }

        if (type is not INamedTypeSymbol { TypeKind: TypeKind.Struct } named) return false;

        if (UnionLayout.IsUnion(named))
        {
            var info = UnionLayout.Analyze(named, new List<Diagnostic>());
            if (info is null) return false;
            size = info.TotalSize;
            align = info.Align;
            return true;
        }

        var offset = 0;
        foreach (var member in StructMembers.Instance(named))
        {
            if (!TrySizeAndAlign(member.Type, out var memberSize, out var memberAlign)) return false;
            offset = AlignUp(offset, memberAlign) + memberSize;
            align = System.Math.Max(align, memberAlign);
        }

        size = AlignUp(offset, align);
        return true;
    }

    public static int AlignUp(int value, int alignment) => (value + alignment - 1) / alignment * alignment;
}
