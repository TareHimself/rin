using System.Numerics;
using Examples.RenderGraphOverlay.Layout;
using Examples.RenderGraphOverlay.Snapshot;

namespace Examples.Tests.RenderGraphOverlay.Layout;

public class GraphLayoutTests
{
    private const float NodeWidth = 100f;

    [Test]
    public void ChainOfPassesStacksStraightDownWithDirectEdges()
    {
        var layout = Compute(Rows([1], [2], [3]), Link(1, 2), Link(2, 3));

        Assert.That(CenterX(layout, 1), Is.EqualTo(CenterX(layout, 2)).Within(0.01f));
        Assert.That(CenterX(layout, 2), Is.EqualTo(CenterX(layout, 3)).Within(0.01f));
        Assert.That(layout.Edges.Select(e => e.Path.Count), Is.All.EqualTo(2));
    }

    [Test]
    public void ResourcesBetweenTheSamePairOfPassesShareOneEdge()
    {
        var layout = Compute(Rows([1], [2]), Link(1, 2, resource: 10), Link(1, 2, resource: 11), Link(1, 2, resource: 12));

        var edge = layout.Edges.Single().Edge;
        Assert.That(edge.ResourceIds, Is.EquivalentTo(new uint[] { 10, 11, 12 }));
    }

    [Test]
    public void EdgeImpliedByALongerPathIsMarkedRedundant()
    {
        var layout = Compute(Rows([1], [2], [3]), Link(1, 2), Link(2, 3), Link(1, 3));

        var redundant = layout.Edges.Where(e => e.Edge.IsRedundant).Select(e => (e.Edge.FromPassId, e.Edge.ToPassId));
        Assert.That(redundant, Is.EqualTo(new[] { (1u, 3u) }));
    }

    [Test]
    public void RedundantEdgeRunsRightOfEveryPass()
    {
        var layout = Compute(Rows([1], [2], [3]), Link(1, 2), Link(2, 3), Link(1, 3));

        var contentRight = layout.Nodes.Values.Max(n => n.Position.X + n.Size.X);
        var redundant = layout.Edges.Single(e => e.Edge.IsRedundant);
        Assert.That(redundant.Path.Max(p => p.X), Is.GreaterThan(contentRight));
    }

    [Test]
    public void LongEdgeRoutesAroundAPassInAnIntermediateRow()
    {
        var layout = Compute(Rows([1], [2, 4], [3]), Link(1, 2), Link(2, 3), Link(1, 4), Link(4, 3), Link(1, 3, resource: 99));

        AssertNoEdgeCrossesAPass(layout);
    }

    [Test]
    public void CrossedConnectionsAreReorderedToRemoveTheCrossing()
    {
        var layout = Compute(Rows([1, 2], [3, 4]), Link(1, 4), Link(2, 3));

        Assert.That(CenterX(layout, 1) < CenterX(layout, 2), Is.EqualTo(CenterX(layout, 4) < CenterX(layout, 3)));
    }

    [Test]
    public void PassesInARowNeverOverlap()
    {
        var layout = Compute(Rows([1, 2, 3, 4], [5, 6], [7]),
            Link(1, 5), Link(2, 5), Link(3, 6), Link(4, 6), Link(5, 7), Link(6, 7));

        foreach (var row in layout.Nodes.Values.Where(n => !n.Pass.IsBarrier).GroupBy(n => n.Position.Y))
        {
            var ordered = row.OrderBy(n => n.Position.X).ToList();
            for (var i = 1; i < ordered.Count; i++)
                Assert.That(ordered[i].Position.X, Is.GreaterThanOrEqualTo(ordered[i - 1].Position.X + ordered[i - 1].Size.X));
        }
    }

    [Test]
    public void RenderPipelineShapedGraphHasNoEdgeThroughAPass()
    {
        var layout = Compute(
            Rows([1, 2, 3], [4, 5], [6, 7], [8, 9], [10, 11], [12]),
            Link(1, 4), Link(2, 4), Link(2, 5), Link(3, 5), Link(4, 6), Link(5, 7), Link(1, 6), Link(2, 7),
            Link(6, 8), Link(7, 9), Link(4, 8), Link(5, 9), Link(8, 10), Link(9, 11), Link(3, 10), Link(10, 12),
            Link(11, 12), Link(1, 12));

        AssertNoEdgeCrossesAPass(layout);
    }

    [Test]
    public void EveryPathUsesOnlyHorizontalAndVerticalSegments()
    {
        var layout = Compute(Rows([1, 2], [3, 4], [5]), Link(1, 3), Link(2, 4), Link(1, 4), Link(3, 5), Link(4, 5), Link(1, 5));

        foreach (var path in layout.Edges.Select(e => e.Path))
            for (var i = 1; i < path.Count; i++)
                Assert.That(path[i].X == path[i - 1].X || path[i].Y == path[i - 1].Y, Is.True);
    }

    [Test]
    public void EdgeStartsAtTheBottomOfItsSourceAndEndsAtTheTopOfItsTarget()
    {
        var layout = Compute(Rows([1, 2], [3]), Link(1, 3), Link(2, 3));

        foreach (var edge in layout.Edges)
        {
            var from = layout.Nodes[edge.Edge.FromPassId];
            var to = layout.Nodes[edge.Edge.ToPassId];
            Assert.That(edge.Path[0].Y, Is.EqualTo(from.Position.Y + from.Size.Y).Within(0.01f));
            Assert.That(edge.Path[^1].Y, Is.EqualTo(to.Position.Y).Within(0.01f));
        }
    }

    [Test]
    public void BarriersSitInTheGutterBetweenTheirRows()
    {
        var snapshot = Snapshot(
            [Stage(0, barrier: true, 100), Stage(1, false, 1), Stage(2, true, 101), Stage(3, false, 2)],
            Link(1, 2));
        var layout = GraphLayout.Compute(snapshot, _ => NodeWidth);

        var firstPass = layout.Nodes[1];
        var secondPass = layout.Nodes[2];
        var barrier = layout.Nodes[101];
        Assert.That(barrier.Position.X + barrier.Size.X, Is.LessThanOrEqualTo(firstPass.Position.X));
        Assert.That(barrier.Position.Y, Is.GreaterThan(firstPass.Position.Y + firstPass.Size.Y));
        Assert.That(barrier.Position.Y + barrier.Size.Y, Is.LessThan(secondPass.Position.Y));
        Assert.That(layout.Nodes[100].Position.Y, Is.GreaterThanOrEqualTo(0f));
    }

    [Test]
    public void EmptySnapshotProducesAnEmptyLayout()
    {
        var layout = GraphLayout.Compute(Snapshot([]), _ => NodeWidth);

        Assert.That(layout.Nodes, Is.Empty);
        Assert.That(layout.Edges, Is.Empty);
    }

    [Test]
    public void HitTestFindsThePassUnderAPoint()
    {
        var layout = Compute(Rows([1], [2]), Link(1, 2));
        var node = layout.Nodes[2];

        Assert.That(layout.HitTest(node.Position + node.Size / 2f)?.Pass.Id, Is.EqualTo(2u));
        Assert.That(layout.HitTest(new Vector2(-500f)), Is.Null);
    }

    private static void AssertNoEdgeCrossesAPass(GraphLayout layout)
    {
        foreach (var edge in layout.Edges.Where(e => !e.Edge.IsRedundant))
        foreach (var node in layout.Nodes.Values.Where(n => n.Pass.Id != edge.Edge.FromPassId && n.Pass.Id != edge.Edge.ToPassId))
            for (var i = 1; i < edge.Path.Count; i++)
                Assert.That(SegmentTouchesNode(edge.Path[i - 1], edge.Path[i], node), Is.False,
                    $"edge {edge.Edge.FromPassId}->{edge.Edge.ToPassId} crosses pass {node.Pass.Id}");
    }

    private static bool SegmentTouchesNode(Vector2 a, Vector2 b, NodeLayout node)
    {
        var minX = float.Min(a.X, b.X);
        var maxX = float.Max(a.X, b.X);
        var minY = float.Min(a.Y, b.Y);
        var maxY = float.Max(a.Y, b.Y);
        return maxX > node.Position.X && minX < node.Position.X + node.Size.X &&
               maxY > node.Position.Y && minY < node.Position.Y + node.Size.Y;
    }

    private static float CenterX(GraphLayout layout, uint passId)
    {
        var node = layout.Nodes[passId];
        return node.Position.X + node.Size.X / 2f;
    }

    private static GraphLayout Compute(StageSnapshot[] stages, params LinkSnapshot[] links)
    {
        return GraphLayout.Compute(Snapshot(stages, links), _ => NodeWidth);
    }

    private static StageSnapshot[] Rows(params uint[][] rows)
    {
        return rows.Select((ids, index) => Stage(index, false, ids)).ToArray();
    }

    private static StageSnapshot Stage(int index, bool barrier, params uint[] passIds)
    {
        var passes = passIds.Select(id => new PassSnapshot(id, $"Pass{id}", PassCategory.World, index, barrier, [], []))
            .ToList();
        return new StageSnapshot(index, barrier ? "Barrier" : $"Stage {index}", barrier, passes);
    }

    private static GraphSnapshot Snapshot(StageSnapshot[] stages, params LinkSnapshot[] links)
    {
        return new GraphSnapshot(DateTime.UnixEpoch, stages, new Dictionary<uint, ResourceSnapshot>(), links, 0);
    }

    private static LinkSnapshot Link(uint from, uint to, uint resource = 1)
    {
        return new LinkSnapshot(resource, from, to);
    }
}
