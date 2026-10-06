namespace Examples.RenderGraphOverlay.Layout;

internal static class CoordinateAssignment
{
    private const int Iterations = 10;

    public static void Apply(List<List<LayoutItem>> rows, float gap)
    {
        PlaceCentered(rows, gap);

        for (var i = 0; i < Iterations; i++)
        {
            for (var r = 1; r < rows.Count; r++) PlaceTowardNeighbors(rows[r], item => item.Up, gap);
            for (var r = rows.Count - 2; r >= 0; r--) PlaceTowardNeighbors(rows[r], item => item.Down, gap);
        }
    }

    public static float RowWidth(List<LayoutItem> row, float gap)
    {
        return row.Sum(item => item.Width) + gap * float.Max(row.Count - 1, 0);
    }

    private static void PlaceCentered(List<List<LayoutItem>> rows, float gap)
    {
        var widest = rows.Count == 0 ? 0f : rows.Max(row => RowWidth(row, gap));
        foreach (var row in rows)
        {
            var x = (widest - RowWidth(row, gap)) / 2f;
            foreach (var item in row)
            {
                item.X = x;
                x += item.Width + gap;
            }
        }
    }

    private static void PlaceTowardNeighbors(List<LayoutItem> row, Func<LayoutItem, List<LayoutItem>> neighbors,
        float gap)
    {
        if (row.Count == 0) return;

        var offsets = new float[row.Count];
        var run = 0f;
        for (var i = 0; i < row.Count; i++)
        {
            offsets[i] = run;
            run += row[i].Width + gap;
        }

        var targets = row.Select((item, i) =>
        {
            var linked = neighbors(item);
            var desired = linked.Count == 0 ? item.X : linked.Average(n => n.CenterX) - item.Width / 2f;
            return desired - offsets[i];
        }).ToArray();

        var shifts = NonDecreasingFit(targets);
        for (var i = 0; i < row.Count; i++) row[i].X = shifts[i] + offsets[i];
    }

    private static float[] NonDecreasingFit(float[] targets)
    {
        List<(float Sum, int Count)> blocks = [];
        foreach (var target in targets)
        {
            blocks.Add((target, 1));
            while (blocks.Count > 1 && Mean(blocks[^2]) > Mean(blocks[^1]))
            {
                var merged = (blocks[^2].Sum + blocks[^1].Sum, blocks[^2].Count + blocks[^1].Count);
                blocks.RemoveRange(blocks.Count - 2, 2);
                blocks.Add(merged);
            }
        }

        return blocks.SelectMany(block => Enumerable.Repeat(Mean(block), block.Count)).ToArray();
    }

    private static float Mean((float Sum, int Count) block)
    {
        return block.Sum / block.Count;
    }
}
