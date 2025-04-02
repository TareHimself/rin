namespace Rin.Shading.Ast.Nodes;

public static class NodeExtensions
{
    public static bool Transverse(this INode node, Func<INode, bool> transverse)
    {
        var items = new Queue<INode>();
        items.Enqueue(node);
        while (items.Count > 0)
        {
            var current = items.Dequeue();
            if (!transverse(current)) return false;

            foreach (var currentChild in current.Children) items.Enqueue(currentChild);
        }

        return true;
    }

    public static bool Transverse(this IEnumerable<INode> nodes, Func<INode, bool> transverse)
    {
        foreach (var nodeStatement in nodes)
            if (!nodeStatement.Transverse(transverse))
                return false;

        return true;
    }
}