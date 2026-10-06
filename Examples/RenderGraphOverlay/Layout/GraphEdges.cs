using RenderGraphOverlay.Snapshot;

namespace RenderGraphOverlay.Layout;

public sealed record GraphEdge(uint FromPassId, uint ToPassId, IReadOnlyList<uint> ResourceIds, bool IsRedundant);

internal static class GraphEdges
{
    public static List<GraphEdge> Build(IReadOnlyList<LinkSnapshot> links, IReadOnlyDictionary<uint, int> rowOf)
    {
        var pairs = links
            .Where(l => rowOf.TryGetValue(l.FromPassId, out var from) && rowOf.TryGetValue(l.ToPassId, out var to) &&
                        from < to)
            .GroupBy(l => (l.FromPassId, l.ToPassId))
            .Select(g => (From: g.Key.FromPassId, To: g.Key.ToPassId, Resources: g.Select(l => l.ResourceId).Distinct().ToList()))
            .ToList();

        var successors = pairs.ToLookup(p => p.From, p => p.To);
        return pairs.Select(p => new GraphEdge(p.From, p.To, p.Resources, IsImpliedByLongerPath(p.From, p.To, successors)))
            .ToList();
    }

    private static bool IsImpliedByLongerPath(uint from, uint to, ILookup<uint, uint> successors)
    {
        var visited = new HashSet<uint>();
        var pending = new Queue<uint>(successors[from].Where(next => next != to));

        while (pending.Count > 0)
        {
            var current = pending.Dequeue();
            if (!visited.Add(current)) continue;

            foreach (var next in successors[current])
            {
                if (next == to) return true;
                pending.Enqueue(next);
            }
        }

        return false;
    }
}
