using System.Linq;
using Microsoft.CodeAnalysis;

namespace Rin.Shade.Transpiler;

/// <summary>
/// A C# [InlineArray(N)] struct is how a shader declares a fixed-size array: N contiguous elements,
/// no heap allocation, and legal anywhere a value type is. It lowers to a Slang T[N] and is never
/// emitted as a struct of its own.
/// </summary>
internal static class InlineArrays
{
    public static bool TryGet(ITypeSymbol type, out ITypeSymbol element, out int length)
    {
        element = null!;
        length = 0;

        if (type is not INamedTypeSymbol { TypeKind: TypeKind.Struct } named) return false;

        var attribute = named.GetAttributes().FirstOrDefault(a =>
            a.AttributeClass?.ToDisplayString() == "System.Runtime.CompilerServices.InlineArrayAttribute");
        if (attribute?.ConstructorArguments.FirstOrDefault().Value is not int declaredLength) return false;

        var elementField = named.GetMembers().OfType<IFieldSymbol>().FirstOrDefault(f => !f.IsStatic);
        if (elementField is null) return false;

        element = elementField.Type;
        length = declaredLength;
        return true;
    }
}
