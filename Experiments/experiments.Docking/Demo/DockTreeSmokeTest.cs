using experiments.Docking.Model;

namespace experiments.Docking.Demo;

/// <summary>
///     Headless exercise of <see cref="DockTree" /> mutations — run with `dotnet run ... test`.
///     Verifies the tree stays well-formed (no cycles, consistent parent pointers, valid
///     weights, every panel present exactly once) across a sequence of moves and closes.
/// </summary>
public static class DockTreeSmokeTest
{
    public static int Run()
    {
        var failures = 0;

        void Check(string what, Func<bool> ok)
        {
            bool passed;
            try
            {
                passed = ok();
            }
            catch (Exception e)
            {
                Console.WriteLine($"  THREW  {what}: {e.GetType().Name}: {e.Message}");
                failures++;
                return;
            }

            Console.WriteLine($"  {(passed ? "ok   " : "FAIL ")}{what}");
            if (!passed) failures++;
        }

        // The tree logic never touches Content, so a null stand-in is fine here.
        DockPanel P(string id) => new(id, id, null!);

        var explorer = P("explorer");
        var outline = P("outline");
        var editorA = P("editorA");
        var editorB = P("editorB");
        var console = P("console");

        var rightSplit = new DockSplitNode(DockOrientation.Vertical,
            new DockTabGroupNode(editorA, editorB),
            new DockTabGroupNode(console));
        var tree = new DockTree(new DockSplitNode(DockOrientation.Horizontal,
            new DockTabGroupNode(explorer, outline),
            rightSplit));

        var expectedIds = new[] { "explorer", "outline", "editorA", "editorB", "console" }.OrderBy(x => x).ToArray();

        Check("initial tree well-formed", () => WellFormed(tree, expectedIds));

        // editorA -> console group as a tab
        var consoleGroup = tree.FindGroup(console)!;
        tree.MovePanel(editorA, consoleGroup, DockRegion.Center);
        Check("after center-drop: well-formed", () => WellFormed(tree, expectedIds));
        Check("after center-drop: editorA with console", () =>
            ReferenceEquals(tree.FindGroup(editorA), tree.FindGroup(console)));

        // editorB -> left edge of explorer group
        tree.MovePanel(editorB, tree.FindGroup(explorer)!, DockRegion.Left);
        Check("after left-drop: well-formed", () => WellFormed(tree, expectedIds));

        // drag the only panel of a group onto its own edge -> no-op, no corruption
        var lonelyGroup = tree.FindGroup(editorB)!;
        var before = Describe(tree);
        tree.MovePanel(editorB, lonelyGroup, DockRegion.Right);
        Check("self-edge drop is a no-op", () => Describe(tree) == before);

        // outline -> bottom of the editorA/console group
        tree.MovePanel(outline, tree.FindGroup(editorA)!, DockRegion.Bottom);
        Check("after bottom-drop: well-formed", () => WellFormed(tree, expectedIds));

        // close panels one by one; tree must always stay well-formed
        foreach (var panel in new[] { console, editorA, outline, explorer })
        {
            tree.ClosePanel(panel);
            var remaining = expectedIds.Where(id => id != panel.Id).OrderBy(x => x).ToArray();
            expectedIds = remaining;
            Check($"after closing {panel.Id}: well-formed", () => WellFormed(tree, remaining));
        }

        // collapse everything
        tree.ClosePanel(editorB);
        Check("after closing all: root is an empty group", () => tree.Root is DockTabGroupNode { Panels.Count: 0 });

        // --- fuzz: many random moves, tree must never corrupt -----------------------------
        var fuzzPanels = Enumerable.Range(0, 6).Select(i => P($"f{i}")).ToArray();
        var fuzzIds = fuzzPanels.Select(p => p.Id).OrderBy(x => x).ToArray();
        var fuzz = new DockTree(new DockTabGroupNode(fuzzPanels));
        var rng = new Random(1234);
        var regions = Enum.GetValues<DockRegion>();
        var fuzzOk = true;
        for (var i = 0; i < 400 && fuzzOk; i++)
        {
            var panel = fuzzPanels[rng.Next(fuzzPanels.Length)];
            var groups = fuzz.AllGroups().ToArray();
            var target = groups[rng.Next(groups.Length)];
            try
            {
                fuzz.MovePanel(panel, target, regions[rng.Next(regions.Length)]);
                fuzzOk = WellFormed(fuzz, fuzzIds);
            }
            catch (Exception e)
            {
                Console.WriteLine($"  fuzz iteration {i} threw {e.GetType().Name}: {e.Message}");
                fuzzOk = false;
            }
        }

        Check("400 random moves keep the tree well-formed", () => fuzzOk);

        Console.WriteLine(failures == 0 ? "\nALL PASSED" : $"\n{failures} FAILURE(S)");
        return failures == 0 ? 0 : 1;
    }

    private static bool WellFormed(DockTree tree, string[] expectedPanelIds)
    {
        var seen = new HashSet<DockNode>();

        bool Visit(DockNode node, DockSplitNode? parent)
        {
            if (!seen.Add(node)) return false; // cycle / shared node
            if (!ReferenceEquals(node.Parent, parent)) return false;

            if (node is DockSplitNode split)
            {
                if (split.Children.Count < 2) return false;
                if (split.Weights.Count != split.Children.Count) return false;
                var sum = split.Weights.Sum();
                if (float.Abs(sum - 1f) > 0.01f) return false;
                if (split.Weights.Any(w => w <= 0f || !float.IsFinite(w))) return false;
                foreach (var child in split.Children)
                    if (!Visit(child, split))
                        return false;
            }

            return true;
        }

        if (!Visit(tree.Root, null)) return false;

        var ids = tree.AllGroups().SelectMany(g => g.Panels).Select(p => p.Id).OrderBy(x => x).ToArray();
        return ids.SequenceEqual(expectedPanelIds);
    }

    private static string Describe(DockNode node)
    {
        return node switch
        {
            DockTabGroupNode g => $"[{string.Join(",", g.Panels.Select(p => p.Id))}]",
            DockSplitNode s =>
                $"({s.Orientation.ToString()[..1]} {string.Join(" | ", s.Children.Zip(s.Weights, (c, w) => $"{Describe(c)}:{w:0.00}"))})",
            _ => "?"
        };
    }

    private static string Describe(DockTree tree) => Describe(tree.Root);
}
