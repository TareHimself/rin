using System.Numerics;

namespace RenderGraphOverlay.Layout;

internal sealed record Chain(GraphEdge Edge, List<LayoutItem> Items);

internal sealed record RoutedEdges(float[] RowTops, float[] GapHeights, List<Vector2[]> Paths);

internal static class EdgeRouter
{
    private const float MinGapHeight = 44f;
    private const float FirstTrackOffset = 16f;
    private const float TrackSpacing = 10f;
    private const float TrackClearance = 6f;
    private const float StraightTolerance = 0.5f;

    private sealed class Hop(LayoutItem from, LayoutItem to)
    {
        public LayoutItem From { get; } = from;
        public LayoutItem To { get; } = to;
        public float StartX { get; set; }
        public float EndX { get; set; }
        public int Track { get; set; } = -1;
        public bool IsStraight => float.Abs(StartX - EndX) < StraightTolerance;
    }

    public static RoutedEdges Route(List<Chain> chains, int rowCount, float rowHeight, float topPadding)
    {
        var hopsPerChain = chains.Select(chain => chain.Items.Zip(chain.Items.Skip(1), (u, v) => new Hop(u, v)).ToList())
            .ToList();
        var hops = hopsPerChain.SelectMany(h => h).ToList();

        AssignAnchors(hops);
        var gapHeights = AssignTracks(hops, rowCount);
        var rowTops = ComputeRowTops(rowCount, rowHeight, topPadding, gapHeights);
        var paths = hopsPerChain.Select(chainHops => BuildPath(chainHops, rowTops, rowHeight)).ToList();
        return new RoutedEdges(rowTops, gapHeights, paths);
    }

    private static void AssignAnchors(List<Hop> hops)
    {
        foreach (var group in hops.GroupBy(h => h.From))
        {
            var ordered = group.OrderBy(h => h.To.CenterX).ToList();
            for (var i = 0; i < ordered.Count; i++) ordered[i].StartX = Spread(group.Key, i, ordered.Count);
        }

        foreach (var group in hops.GroupBy(h => h.To))
        {
            var ordered = group.OrderBy(h => h.From.CenterX).ToList();
            for (var i = 0; i < ordered.Count; i++) ordered[i].EndX = Spread(group.Key, i, ordered.Count);
        }
    }

    private static float Spread(LayoutItem item, int slot, int count)
    {
        return item.X + item.Width * (slot + 1) / (count + 1);
    }

    private static float[] AssignTracks(List<Hop> hops, int rowCount)
    {
        var gapHeights = new float[int.Max(rowCount - 1, 0)];
        for (var row = 0; row < gapHeights.Length; row++)
        {
            var bends = hops.Where(h => h.From.Row == row && !h.IsStraight)
                .OrderBy(h => float.Min(h.StartX, h.EndX)).ToList();
            List<float> trackEnds = [];

            foreach (var hop in bends)
            {
                var left = float.Min(hop.StartX, hop.EndX);
                var track = trackEnds.FindIndex(end => end + TrackClearance < left);
                if (track < 0)
                {
                    trackEnds.Add(0f);
                    track = trackEnds.Count - 1;
                }

                trackEnds[track] = float.Max(hop.StartX, hop.EndX);
                hop.Track = track;
            }

            gapHeights[row] = MinGapHeight + float.Max(trackEnds.Count - 1, 0) * TrackSpacing;
        }

        return gapHeights;
    }

    private static float[] ComputeRowTops(int rowCount, float rowHeight, float topPadding, float[] gapHeights)
    {
        var tops = new float[rowCount];
        var y = topPadding;
        for (var row = 0; row < rowCount; row++)
        {
            tops[row] = y;
            y += rowHeight + (row < gapHeights.Length ? gapHeights[row] : 0f);
        }

        return tops;
    }

    private static Vector2[] BuildPath(List<Hop> hops, float[] rowTops, float rowHeight)
    {
        List<Vector2> points = [new(hops[0].StartX, rowTops[hops[0].From.Row] + rowHeight)];

        for (var i = 0; i < hops.Count; i++)
        {
            var hop = hops[i];
            var row = hop.From.Row;

            if (!hop.IsStraight)
            {
                var trackY = rowTops[row] + rowHeight + FirstTrackOffset + hop.Track * TrackSpacing;
                points.Add(new Vector2(hop.StartX, trackY));
                points.Add(new Vector2(hop.EndX, trackY));
            }

            points.Add(new Vector2(hop.EndX, rowTops[row + 1]));
            if (i < hops.Count - 1) points.Add(new Vector2(hop.EndX, rowTops[row + 1] + rowHeight));
        }

        return Simplify(points);
    }

    private static Vector2[] Simplify(List<Vector2> points)
    {
        List<Vector2> result = [];
        foreach (var point in points)
        {
            if (result.Count > 0 && Vector2.DistanceSquared(result[^1], point) < 0.0001f) continue;

            while (result.Count >= 2 && IsStraightThrough(result[^2], result[^1], point)) result.RemoveAt(result.Count - 1);
            result.Add(point);
        }

        return result.ToArray();
    }

    private static bool IsStraightThrough(Vector2 a, Vector2 b, Vector2 c)
    {
        return (float.Abs(a.X - b.X) < StraightTolerance && float.Abs(b.X - c.X) < StraightTolerance) ||
               (float.Abs(a.Y - b.Y) < StraightTolerance && float.Abs(b.Y - c.Y) < StraightTolerance);
    }
}
