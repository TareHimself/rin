using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Rin.Shade.Transpiler;

/// <summary>
/// Lowers a C# struct to a Slang struct, including semantics and nested type declarations.
/// </summary>
internal static class StructLowering
{
    /// <summary>
    /// The Slang struct declaration. Takes the given members (default: the struct's instance members) and
    /// the already-lowered text of nested types to print inside its body. Unsupported fields are reported and skipped.
    /// </summary>
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

    /// <summary>
    /// The Slang semantic from [Semantic] or an attribute deriving from it (its SemanticName plus an optional index).
    /// </summary>
    private static string? GetSemantic(IFieldSymbol field)
    {
        foreach (var attribute in field.GetAttributes())
        {
            if (attribute.AttributeClass is not { } type) continue;

            if (type.ToDisplayString() == "Rin.Shade.SemanticAttribute")
                return attribute.ConstructorArguments.FirstOrDefault().Value as string;

            if (!DerivesFromSemantic(type)) continue;

            var name = type.GetMembers("SemanticName").OfType<IFieldSymbol>()
                .FirstOrDefault(f => f.IsConst)?.ConstantValue as string;
            if (name is null) continue;

            return attribute.ConstructorArguments.FirstOrDefault().Value is int index ? name + index : name;
        }

        return null;
    }

    private static bool DerivesFromSemantic(INamedTypeSymbol type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
            if (current.ToDisplayString() == "Rin.Shade.SemanticAttribute")
                return true;

        return false;
    }
}
