using System.Collections.Generic;
using System.Linq;

namespace Rin.Shade.Transpiler;

/// <summary>
/// One namespace block of the emitted file: the declarations written directly in it and the namespaces
/// nested in it, in the order they are printed. An entry is either a declaration's text or a child.
/// </summary>
internal sealed class NamespaceNode(string[] path)
{
    /// <summary>The segments of the `namespace a::b` line, relative to the node it is nested in.</summary>
    public string[] Path { get; } = path;

    public List<(string? Text, NamespaceNode? Child)> Entries { get; } = [];
}

/// <summary>
/// The merge step between deciding which namespace each declaration lives in and printing them. Takes the
/// declarations in their final order, each with its full namespace path, and nests neighbouring runs that
/// share a prefix under it, one segment per block (namespace Rin { namespace Core { ... } }), because Slang
/// resolves a relative name only through the namespaces lexically open around it. It never reorders: Slang
/// needs a type declared before it is used, so only declarations that are already adjacent get grouped.
/// The result is a tree for the emitter to print - a declaration with no namespace sits at the root.
/// </summary>
internal static class NamespaceTree
{
    public static List<(string? Text, NamespaceNode? Child)> Build(
        IReadOnlyList<(string[] Path, string Text)> declarations)
    {
        var entries = new List<(string? Text, NamespaceNode? Child)>();
        var index = 0;

        while (index < declarations.Count)
        {
            var declaration = declarations[index];
            if (declaration.Path.Length == 0)
            {
                entries.Add((declaration.Text, null));
                index++;
                continue;
            }

            var end = index + 1;
            while (end < declarations.Count && declarations[end].Path.Length > 0 &&
                   declarations[end].Path[0] == declaration.Path[0])
                end++;

            var run = declarations.Skip(index).Take(end - index).ToList();

            var node = new NamespaceNode(declaration.Path[..1]);
            node.Entries.AddRange(Build(run.Select(d => (d.Path[1..], d.Text)).ToList()));
            entries.Add((null, node));

            index = end;
        }

        return entries;
    }
}
