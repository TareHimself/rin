using Microsoft.CodeAnalysis;

namespace Rin.Shade.Transpiler;

/// <summary>
/// Maps C# types to their Slang spellings and classifies which types a shader may use.
/// </summary>
internal static class TypeMapping
{
    /// <summary>
    /// The Slang spelling of the type, qualified relative to the current <see cref="NameScope"/>.
    /// </summary>
    public static string MapType(ITypeSymbol type)
    {
        if (InlineArrays.TryGet(type, out var inlineElement, out _)) return MapType(inlineElement);
        if (UnionLayout.IsUnion(type)) return Naming.Qualify(type, $"{Naming.GeneratedPrefix}Complete{type.Name}");

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

        // Slang's builtin resource types are global, so the C# marker type's Rin::Shade namespace must not be applied.
        if (IsResourceType(type)) return type.Name;

        return Naming.Qualify(type, type.Name);
    }

    /// <summary>
    /// Whether the type is a BCL vector or matrix, whose constructors have no source to lower and are forwarded as is.
    /// </summary>
    public static bool IsIntrinsicVectorOrMatrixConstructor(ITypeSymbol type) =>
        type.OriginalDefinition.ToDisplayString() is
            "System.Numerics.Vector2" or "System.Numerics.Vector3" or
            "System.Numerics.Vector4" or "System.Numerics.Matrix4x4";

    /// <summary>
    /// Whether the type is known to Slang already, so it is never declared or walked as a user struct.
    /// </summary>
    public static bool IsBuiltIn(ITypeSymbol type)
    {
        if (InlineArrays.TryGet(type, out _, out _)) return true;

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

        if (IsResourceType(type)) return true;

        return type is INamedTypeSymbol { Name: "BufferRef" };
    }

    /// <summary>
    /// Whether the type is a Slang builtin opaque resource (texture or sampler), a plain C# struct on the Rin.Shade side.
    /// </summary>
    public static bool IsResourceType(ITypeSymbol type) =>
        type.OriginalDefinition.ToDisplayString() is
            "Rin.Shade.Texture2D" or "Rin.Shade.Texture2DArray" or
            "Rin.Shade.TextureCube" or "Rin.Shade.SamplerState";

    /// <summary>
    /// Whether the type is legal in a shader at all. Stricter than IsBuiltIn: it rejects strings, delegates
    /// and other reference types that MapType would otherwise emit by bare name.
    /// </summary>
    public static bool IsLegalShaderType(ITypeSymbol type)
    {
        if (InlineArrays.TryGet(type, out var inlineElement, out _)) return IsLegalShaderType(inlineElement);
        if (IsBuiltIn(type)) return true;
        return type.TypeKind is TypeKind.Struct or TypeKind.Enum;
    }
}
