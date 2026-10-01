using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Rin.Shade.Transpiler;

/// <summary>
/// Prints a TypeGraph's nodes as Slang declarations, each inside the namespace its C# type lives in. A run
/// of types in the same namespace shares one block, and neighbouring blocks that share a namespace prefix
/// are wrapped in it (see NamespaceTree, which decides the nesting; this prints it). A type nested in a
/// struct is printed inside that struct's body.
/// </summary>
internal static class TypeEmitter
{
    /// <summary>
    /// Every type and namespace the graph declares, by full path, so a short name can be checked against them.
    /// </summary>
    public static HashSet<string> Declared(TypeGraph graph)
    {
        var declared = new HashSet<string>();

        void Declare(string path)
        {
            for (var current = path; current.Length > 0;)
            {
                declared.Add(current);
                var separator = current.LastIndexOf("::", System.StringComparison.Ordinal);
                current = separator < 0 ? "" : current[..separator];
            }
        }

        void Visit(TypeNode node)
        {
            var type = node.Symbol;
            Declare(FullPath(type, type.Name));
            if (UnionLayout.IsUnion(type)) Declare(FullPath(type, $"{Naming.GeneratedPrefix}Complete{type.Name}"));
            foreach (var nested in node.Nested) Visit(nested);
        }

        foreach (var node in graph.Ordered()) Visit(node);
        return declared;
    }

    /// <summary>
    /// Writes every type of the graph, in dependency order, grouped into namespace blocks.
    /// </summary>
    public static void Emit(TypeGraph graph, List<Diagnostic> diagnostics, SlangWriter writer, HashSet<string> declared)
    {
        var declarations = new List<(string[] Path, string Text)>();

        foreach (var node in graph.Ordered())
        {
            var text = EmitNode(node, diagnostics, declared);
            if (text.Length == 0) continue;

            var path = Naming.NamespacePath(node.Symbol);
            declarations.Add((path.Length == 0 ? [] : path.Split(["::"], System.StringSplitOptions.None), text));
        }

        Print(NamespaceTree.Build(declarations), writer, 0);
    }

    private static void Print(List<(string? Text, NamespaceNode? Child)> entries, SlangWriter writer, int depth)
    {
        foreach (var (text, child) in entries)
        {
            if (child is null)
            {
                if (depth == 0) writer.Append(text!);
                else writer.AppendBlock(text!);
                writer.Line();
                continue;
            }

            writer.OpenBrace($"namespace {string.Join("::", child.Path)}");
            Print(child.Entries, writer, depth + 1);
            writer.CloseBrace().Line();
        }
    }

    private static string FullPath(ISymbol type, string name)
    {
        var path = Naming.NamespacePath(type);
        return path.Length == 0 ? name : $"{path}::{name}";
    }

    private static string EmitNode(TypeNode node, List<Diagnostic> diagnostics, HashSet<string> declared)
    {
        var type = node.Symbol;
        if (type.IsGenericType)
        {
            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.GenericNotSupported,
                type.Locations.FirstOrDefault() ?? Location.None, type.Name));
            return "";
        }

        // A union's variant structs are declared beside the root, not inside it, so what they name that is
        // nested in the root (Quad::LineData) must keep its qualifier.
        using var scope = NameScope.InNamespace(
            UnionLayout.IsUnion(type) ? Naming.NamespacePath(type) : FullPath(type, type.Name), declared);

        var nested = node.Nested.Select(n => EmitNode(n, diagnostics, declared)).Where(text => text.Length > 0).ToList();

        return type.TypeKind == TypeKind.Enum
            ? EnumLowering.Lower(type, diagnostics)
            : UnionLayout.IsUnion(type)
                ? UnionLowering.Lower(type, diagnostics, nested)
                : StructLowering.Lower(type, diagnostics, nested: nested);
    }
}
