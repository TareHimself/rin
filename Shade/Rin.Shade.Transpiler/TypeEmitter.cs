using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Rin.Shade.Transpiler;

/// <summary>
/// Prints a TypeGraph's nodes as Slang declarations, each inside the namespace its C# type lives in
/// (a run of types in the same namespace shares one block). A type nested in a struct is printed
/// inside that struct's body.
/// </summary>
internal static class TypeEmitter
{
    public static void Emit(TypeGraph graph, List<Diagnostic> diagnostics, SlangWriter writer)
    {
        var openNamespace = "";

        void Switch(string path)
        {
            if (path == openNamespace) return;
            if (openNamespace.Length > 0) writer.CloseBrace().Line();
            if (path.Length > 0) writer.OpenBrace($"namespace {path}");
            openNamespace = path;
        }

        foreach (var node in graph.Ordered())
        {
            var text = EmitNode(node, diagnostics);
            if (text.Length == 0) continue;

            Switch(Naming.NamespacePath(node.Symbol));
            if (openNamespace.Length > 0)
                writer.AppendBlock(text);
            else
                writer.Append(text);
            writer.Line();
        }

        Switch("");
    }

    private static string EmitNode(TypeNode node, List<Diagnostic> diagnostics)
    {
        var type = node.Symbol;
        if (type.IsGenericType)
        {
            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.GenericNotSupported,
                type.Locations.FirstOrDefault() ?? Location.None, type.Name));
            return "";
        }

        var nested = node.Nested.Select(n => EmitNode(n, diagnostics)).Where(text => text.Length > 0).ToList();

        return type.TypeKind == TypeKind.Enum
            ? EnumLowering.Lower(type, diagnostics)
            : UnionLayout.IsUnion(type)
                ? UnionLowering.Lower(type, diagnostics, nested)
                : StructLowering.Lower(type, diagnostics, nested: nested);
    }
}
