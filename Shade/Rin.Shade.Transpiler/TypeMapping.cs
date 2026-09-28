using Microsoft.CodeAnalysis;

namespace Rin.Shade.Transpiler;

internal static class TypeMapping
{
    public static string MapType(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol arrayType) return MapType(arrayType.ElementType);

        switch (type.SpecialType)
        {
            case SpecialType.System_Int32: return "int";
            case SpecialType.System_UInt32: return "uint";
            case SpecialType.System_Single: return "float";
            case SpecialType.System_Boolean: return "bool";
            case SpecialType.System_Void: return "void";
        }

        switch (type.OriginalDefinition.ToDisplayString())
        {
            case "System.Numerics.Vector2": return "float2";
            case "System.Numerics.Vector3": return "float3";
            case "System.Numerics.Vector4": return "float4";
            case "System.Numerics.Matrix4x4": return "float4x4";
        }

        if (type is INamedTypeSymbol { Name: "BufferRef", TypeArguments.Length: 1 } named)
            return $"{MapType(named.TypeArguments[0])}*";

        return type.Name;
    }

    /// <summary>
    /// Vector2/3/4 and Matrix4x4 are BCL types - their constructors have no source in this
    /// compilation and can't carry a [SlangCall] binding (we don't own the type), so they'd
    /// otherwise fail the generic "has source or binding" check every other constructor goes
    /// through. Slang's own float2/3/4/float4x4 constructors accept the same positional-argument
    /// shapes (component lists, or a smaller vector plus scalars), so the arguments are trusted to
    /// forward as-is - same trust boundary as the BCL operator-method passthrough for these types.
    /// </summary>
    public static bool IsIntrinsicVectorOrMatrixConstructor(ITypeSymbol type) =>
        type.OriginalDefinition.ToDisplayString() is
            "System.Numerics.Vector2" or "System.Numerics.Vector3" or
            "System.Numerics.Vector4" or "System.Numerics.Matrix4x4";

    public static bool IsBuiltIn(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol) return true;

        switch (type.SpecialType)
        {
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Single:
            case SpecialType.System_Boolean:
            case SpecialType.System_Void:
                return true;
        }

        switch (type.OriginalDefinition.ToDisplayString())
        {
            case "System.Numerics.Vector2":
            case "System.Numerics.Vector3":
            case "System.Numerics.Vector4":
            case "System.Numerics.Matrix4x4":
                return true;
        }

        return type is INamedTypeSymbol { Name: "BufferRef" };
    }

    /// <summary>
    /// Whether a type is legal anywhere in a shader at all - a stricter check than IsBuiltIn, which
    /// only tells the walk "don't recurse into this, it's already known". Used at struct-field
    /// declaration sites to catch nonsense (string, delegates, arbitrary reference types) instead of
    /// silently falling through to `type.Name`.
    /// </summary>
    public static bool IsLegalShaderType(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol arrayType) return IsLegalShaderType(arrayType.ElementType);
        if (IsBuiltIn(type)) return true;
        return type.TypeKind is TypeKind.Struct or TypeKind.Enum;
    }
}
