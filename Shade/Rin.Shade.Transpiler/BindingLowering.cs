using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Rin.Shade.Transpiler;

// [ShaderBinding] fields lower to module-scope Slang declarations (`[[vk::binding(b, s)]] uniform
// ...;`), the same class of emission as [Push] - not struct members, unlike [ShaderStruct] fields.
internal static class BindingLowering
{
    public static bool HasShaderBindingAttribute(IFieldSymbol field) =>
        field.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "Rin.Shade.ShaderBindingAttribute");

    public static string? Lower(Compilation compilation, IFieldSymbol field, List<Diagnostic> diagnostics,
        HashSet<(int Set, int Binding)> seenBindings)
    {
        var location = field.Locations.FirstOrDefault() ?? Location.None;

        if (!field.IsStatic)
        {
            Diagnose(diagnostics, field, location, "[ShaderBinding] fields must be static");
            return null;
        }

        var attribute = field.GetAttributes()
            .First(a => a.AttributeClass?.ToDisplayString() == "Rin.Shade.ShaderBindingAttribute");
        var set = GetNamedInt(attribute, "Set");
        var binding = GetNamedInt(attribute, "Binding");

        if (!seenBindings.Add((set, binding)))
        {
            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.DuplicateBinding, location,
                field.Name, set, binding));
            return null;
        }

        var (resourceType, count) = ResolveShape(compilation, field, diagnostics, location);
        if (resourceType is null) return null;

        var slangType = TypeMapping.MapType(resourceType);
        var name = Naming.ToSlangIdentifier(field.Name);
        var arraySuffix = count is { } n ? $"[{n}]" : "";
        return $"[[vk::binding({binding}, {set})]] uniform {slangType} {name}{arraySuffix};";
    }

    private static (ITypeSymbol? ResourceType, int? Count) ResolveShape(Compilation compilation,
        IFieldSymbol field, List<Diagnostic> diagnostics, Location location)
    {
        if (field.Type is IArrayTypeSymbol arrayType)
        {
            if (!TypeMapping.IsResourceType(arrayType.ElementType))
            {
                Diagnose(diagnostics, field, location,
                    $"array element type '{arrayType.ElementType.ToDisplayString()}' is not a known resource type");
                return (null, null);
            }

            var count = GetConstantArraySize(compilation, field);
            if (count is null)
            {
                Diagnose(diagnostics, field, location,
                    "array size must be a compile-time constant (e.g. `new Texture2D[8]`)");
                return (null, null);
            }

            return (arrayType.ElementType, count);
        }

        if (!TypeMapping.IsResourceType(field.Type))
        {
            Diagnose(diagnostics, field, location,
                $"'{field.Type.ToDisplayString()}' is not a known resource type or an array of one");
            return (null, null);
        }

        return (field.Type, null);
    }

    private static int? GetConstantArraySize(Compilation compilation, IFieldSymbol field)
    {
        foreach (var reference in field.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax() is not VariableDeclaratorSyntax { Initializer.Value: var valueSyntax })
                continue;

            var model = compilation.GetSemanticModel(valueSyntax.SyntaxTree);
            if (model.GetOperation(valueSyntax) is IArrayCreationOperation { DimensionSizes: [var sizeOperation] } &&
                sizeOperation.ConstantValue is { HasValue: true, Value: int size })
                return size;
        }

        return null;
    }

    private static int GetNamedInt(AttributeData attribute, string name) =>
        attribute.NamedArguments.FirstOrDefault(a => a.Key == name).Value.Value is int value ? value : 0;

    private static void Diagnose(List<Diagnostic> diagnostics, IFieldSymbol field, Location location, string reason) =>
        diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedBindingField, location, field.Name, reason));
}
