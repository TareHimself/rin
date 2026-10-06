using System.Numerics;
using RenderGraphOverlay.Snapshot;

namespace RenderGraphOverlay.Layout;

public sealed record NodeLayout(PassSnapshot Pass, Vector2 Position, Vector2 Size)
{
    public bool Contains(Vector2 point)
    {
        return point.X >= Position.X && point.X <= Position.X + Size.X &&
               point.Y >= Position.Y && point.Y <= Position.Y + Size.Y;
    }
}

public sealed record StageLayout(StageSnapshot Stage, float Y, float Height);

public sealed record EdgeLayout(GraphEdge Edge, IReadOnlyList<Vector2> Path);

public sealed class GraphLayout
{
    public const float PassHeight = 46f;
    public const float BarrierHeight = 24f;
    public const float GutterWidth = 120f;
    private const float NodeGapX = 40f;
    private const float NodePaddingX = 14f;
    private const float MinNodeWidth = 150f;
    private const float DummyWidth = 10f;
    private const float LaneStartGap = 36f;
    private const float LaneGap = 14f;
    private const float LaneMargin = 12f;
    private const float FirstBarrierPadding = 40f;

    private GraphLayout(IReadOnlyList<StageLayout> stages, IReadOnlyDictionary<uint, NodeLayout> nodes,
        IReadOnlyList<EdgeLayout> edges, Vector2 size)
    {
        Stages = stages;
        Nodes = nodes;
        Edges = edges;
        Size = size;
    }

    public IReadOnlyList<StageLayout> Stages { get; }
    public IReadOnlyDictionary<uint, NodeLayout> Nodes { get; }
    public IReadOnlyList<EdgeLayout> Edges { get; }
    public Vector2 Size { get; }

    public static GraphLayout Compute(GraphSnapshot snapshot, Func<string, float> measureNodeTitle)
    {
        var passStages = snapshot.Stages.Where(s => !s.IsBarrier).ToList();
        var rowOf = passStages.Select((stage, row) => (stage, row))
            .SelectMany(entry => entry.stage.Passes.Select(pass => (pass.Id, entry.row)))
            .ToDictionary(entry => entry.Id, entry => entry.row);
        var barrierBeforeRow = FindBarriers(snapshot.Stages, passStages.Count);

        var edges = GraphEdges.Build(snapshot.Links, rowOf);
        var (rows, chains) = BuildLayers(passStages, edges.Where(e => !e.IsRedundant).ToList(), measureNodeTitle);

        LayerOrdering.Apply(rows);
        CoordinateAssignment.Apply(rows, NodeGapX);
        ShiftToGutter(rows);

        var topPadding = barrierBeforeRow.ContainsKey(0) ? FirstBarrierPadding : 0f;
        var routed = EdgeRouter.Route(chains, rows.Count, PassHeight, topPadding);

        Dictionary<uint, NodeLayout> nodes = [];
        foreach (var item in rows.SelectMany(row => row).Where(item => !item.IsDummy))
        {
            var pass = passStages[item.Row].Passes.First(p => p.Id == item.PassId);
            nodes[pass.Id] = new NodeLayout(pass, new Vector2(item.X, routed.RowTops[item.Row]),
                new Vector2(item.Width, PassHeight));
        }

        foreach (var (row, barrier) in barrierBeforeRow)
            nodes[barrier.Id] = new NodeLayout(barrier, new Vector2(0f, BarrierY(row, routed)),
                new Vector2(GutterWidth - 12f, BarrierHeight));

        var stages = passStages.Select((stage, row) => new StageLayout(stage, routed.RowTops[row], PassHeight)).ToList();
        var contentRight = nodes.Values.Where(n => !n.Pass.IsBarrier).Select(n => n.Position.X + n.Size.X)
            .DefaultIfEmpty(GutterWidth).Max();

        List<EdgeLayout> edgeLayouts = chains
            .Select((chain, i) => new EdgeLayout(chain.Edge, routed.Paths[i])).ToList();
        var laneCount = AddRedundantEdges(edges.Where(e => e.IsRedundant).ToList(), rowOf, nodes, routed, contentRight,
            edgeLayouts);

        var width = contentRight + (laneCount == 0 ? 0f : LaneStartGap + laneCount * LaneGap);
        var height = rows.Count == 0 ? 0f : routed.RowTops[^1] + PassHeight;
        return new GraphLayout(stages, nodes, edgeLayouts, new Vector2(width, height));
    }

    private static Dictionary<int, PassSnapshot> FindBarriers(IReadOnlyList<StageSnapshot> stages, int rowCount)
    {
        Dictionary<int, PassSnapshot> barriers = [];
        var row = 0;
        foreach (var stage in stages)
        {
            if (stage.IsBarrier && row < rowCount) barriers[row] = stage.Passes[0];
            else row++;
        }

        return barriers;
    }

    private static (List<List<LayoutItem>> Rows, List<Chain> Chains) BuildLayers(List<StageSnapshot> passStages,
        List<GraphEdge> edges, Func<string, float> measureNodeTitle)
    {
        List<List<LayoutItem>> rows = [];
        Dictionary<uint, LayoutItem> items = [];

        for (var row = 0; row < passStages.Count; row++)
        {
            rows.Add([]);
            foreach (var pass in passStages[row].Passes)
            {
                var width = float.Max(MinNodeWidth, measureNodeTitle(pass.Name) + NodePaddingX * 2);
                var item = new LayoutItem { Row = row, Width = width, PassId = pass.Id };
                items[pass.Id] = item;
                rows[row].Add(item);
            }
        }

        List<Chain> chains = [];
        foreach (var edge in edges)
        {
            var from = items[edge.FromPassId];
            var to = items[edge.ToPassId];
            List<LayoutItem> chainItems = [from];

            for (var row = from.Row + 1; row < to.Row; row++)
            {
                var dummy = new LayoutItem { Row = row, Width = DummyWidth, IsDummy = true };
                rows[row].Add(dummy);
                chainItems.Add(dummy);
            }

            chainItems.Add(to);
            for (var i = 0; i < chainItems.Count - 1; i++)
            {
                chainItems[i].Down.Add(chainItems[i + 1]);
                chainItems[i + 1].Up.Add(chainItems[i]);
            }

            chains.Add(new Chain(edge, chainItems));
        }

        return (rows, chains);
    }

    private static void ShiftToGutter(List<List<LayoutItem>> rows)
    {
        var items = rows.SelectMany(row => row).ToList();
        if (items.Count == 0) return;

        var shift = GutterWidth - items.Min(item => item.X);
        foreach (var item in items) item.X += shift;
    }

    private static float BarrierY(int row, RoutedEdges routed)
    {
        return row == 0
            ? routed.RowTops[0] - FirstBarrierPadding + (FirstBarrierPadding - BarrierHeight) / 2f
            : routed.RowTops[row] - routed.GapHeights[row - 1] / 2f - BarrierHeight / 2f;
    }

    private static int AddRedundantEdges(List<GraphEdge> redundant, Dictionary<uint, int> rowOf,
        Dictionary<uint, NodeLayout> nodes, RoutedEdges routed, float contentRight, List<EdgeLayout> output)
    {
        List<int> laneEndRows = [];
        foreach (var edge in redundant.OrderBy(e => rowOf[e.FromPassId]).ThenBy(e => rowOf[e.ToPassId]))
        {
            var from = nodes[edge.FromPassId];
            var to = nodes[edge.ToPassId];
            var lane = AssignLane(laneEndRows, rowOf[edge.FromPassId], rowOf[edge.ToPassId]);
            var laneX = contentRight + LaneStartGap + lane * LaneGap;
            var startX = from.Position.X + from.Size.X / 2f;
            var endX = to.Position.X + to.Size.X / 2f;
            var exitY = from.Position.Y + from.Size.Y + LaneMargin;
            var entryY = to.Position.Y - LaneMargin;

            output.Add(new EdgeLayout(edge,
            [
                new Vector2(startX, from.Position.Y + from.Size.Y), new Vector2(startX, exitY),
                new Vector2(laneX, exitY), new Vector2(laneX, entryY), new Vector2(endX, entryY),
                new Vector2(endX, to.Position.Y)
            ]));
        }

        return laneEndRows.Count;
    }

    private static int AssignLane(List<int> laneEndRows, int fromRow, int toRow)
    {
        var lane = laneEndRows.FindIndex(endRow => endRow < fromRow);
        if (lane < 0)
        {
            laneEndRows.Add(toRow);
            return laneEndRows.Count - 1;
        }

        laneEndRows[lane] = toRow;
        return lane;
    }

    public NodeLayout? HitTest(Vector2 point)
    {
        return Nodes.Values.FirstOrDefault(n => n.Contains(point));
    }
}
