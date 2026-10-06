namespace RenderGraphOverlay.Layout;

internal static class LayerOrdering
{
    private const int Sweeps = 8;

    public static void Apply(List<List<LayoutItem>> rows)
    {
        AssignOrders(rows);
        var best = rows.Select(row => row.ToList()).ToList();
        var bestCrossings = CountCrossings(rows);

        for (var sweep = 0; sweep < Sweeps && bestCrossings > 0; sweep++)
        {
            for (var r = 1; r < rows.Count; r++) SortByBarycenter(rows[r], item => item.Up);
            for (var r = rows.Count - 2; r >= 0; r--) SortByBarycenter(rows[r], item => item.Down);

            var crossings = CountCrossings(rows);
            if (crossings >= bestCrossings) continue;

            bestCrossings = crossings;
            best = rows.Select(row => row.ToList()).ToList();
        }

        for (var r = 0; r < rows.Count; r++) rows[r] = best[r];
        AssignOrders(rows);
    }

    public static int CountCrossings(List<List<LayoutItem>> rows)
    {
        var crossings = 0;
        for (var r = 0; r < rows.Count - 1; r++)
        {
            var edges = rows[r].SelectMany(u => u.Down.Select(v => (From: u.Order, To: v.Order))).ToList();
            for (var i = 0; i < edges.Count; i++)
            for (var j = i + 1; j < edges.Count; j++)
                if ((edges[i].From - edges[j].From) * (edges[i].To - edges[j].To) < 0)
                    crossings++;
        }

        return crossings;
    }

    private static void SortByBarycenter(List<LayoutItem> row, Func<LayoutItem, List<LayoutItem>> neighbors)
    {
        var sorted = row.Select((item, index) => (Item: item, Index: index, Key: Barycenter(item, neighbors(item))))
            .OrderBy(entry => entry.Key).ThenBy(entry => entry.Index).Select(entry => entry.Item).ToList();

        row.Clear();
        row.AddRange(sorted);
        for (var i = 0; i < row.Count; i++) row[i].Order = i;
    }

    private static float Barycenter(LayoutItem item, List<LayoutItem> neighbors)
    {
        return neighbors.Count == 0 ? item.Order : (float)neighbors.Average(n => n.Order);
    }

    private static void AssignOrders(List<List<LayoutItem>> rows)
    {
        foreach (var row in rows)
            for (var i = 0; i < row.Count; i++)
                row[i].Order = i;
    }
}
