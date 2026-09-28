using System.Linq;
using Microsoft.CodeAnalysis;

namespace Rin.Shade.Transpiler;

internal static class FixedSizeAttributeReader
{
    public static int? GetSize(IFieldSymbol field) =>
        field.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "Rin.Shade.FixedSizeAttribute")
            ?.ConstructorArguments.FirstOrDefault().Value as int?;
}
