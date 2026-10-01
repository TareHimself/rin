using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Rin.Shade.Transpiler;

internal static class StructLowering
{
    public static string Lower(INamedTypeSymbol type, List<Diagnostic> diagnostics,
        IEnumerable<StructMember>? members = null, IReadOnlyList<string>? nested = null)
    {
        var writer = new SlangWriter();
        writer.OpenBrace($"struct {type.Name}");

        foreach (var member in members ?? StructMembers.Instance(type))
        {
            var field = member.Field;
            if (field.Type is IArrayTypeSymbol)
            {
                diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedType,
                    field.Locations.FirstOrDefault() ?? Location.None,
                    $"{field.Name} (array field, declare an [InlineArray] struct instead)"));
                continue;
            }

            if (!TypeMapping.IsLegalShaderType(field.Type))
            {
                diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedType,
                    field.Locations.FirstOrDefault() ?? Location.None, field.Type.ToDisplayString()));
                continue;
            }

            var semantic = GetSemantic(field);
            var identifier = Naming.ToSlangIdentifier(member.Name) + (InlineArrays.TryGet(field.Type, out _, out var length) ? $"[{length}]" : "");
            var line = $"{TypeMapping.MapType(field.Type)} {identifier}";
            if (semantic is not null) line += $" : {semantic}";
            writer.Line(line + ";");
        }

        foreach (var nestedText in nested ?? [])
        {
            writer.Line();
            writer.AppendBlock(nestedText);
        }

        writer.CloseBrace();
        return writer.ToString();
    }

    private static string? GetSemantic(IFieldSymbol field) =>
        field.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "Rin.Shade.SemanticAttribute")
            ?.ConstructorArguments.FirstOrDefault().Value as string;
}
