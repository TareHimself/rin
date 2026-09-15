namespace experiments.Docking.Model;

/// <summary>
///     Owns the dock <see cref="Root" /> and all structural mutations. Every method here is a
///     pure tree edit — no views are touched. <see cref="DockSpaceView" /> rebuilds the view
///     tree after calling into this.
/// </summary>
public sealed class DockTree
{
    public DockTree(DockNode root)
    {
        Root = root;
        Root.Parent = null;
    }

    public DockNode Root { get; private set; }

    public DockTabGroupNode? FindGroup(DockPanel panel)
    {
        return Find(Root);

        DockTabGroupNode? Find(DockNode node)
        {
            switch (node)
            {
                case DockTabGroupNode g:
                    return g.Panels.Contains(panel) ? g : null;
                case DockSplitNode s:
                    foreach (var child in s.Children)
                        if (Find(child) is { } hit)
                            return hit;
                    break;
            }

            return null;
        }
    }

    public IEnumerable<DockTabGroupNode> AllGroups()
    {
        return Walk(Root);

        static IEnumerable<DockTabGroupNode> Walk(DockNode node)
        {
            switch (node)
            {
                case DockTabGroupNode g:
                    yield return g;
                    break;
                case DockSplitNode s:
                    foreach (var child in s.Children)
                    foreach (var g in Walk(child))
                        yield return g;
                    break;
            }
        }
    }

    /// <summary>Move an already-docked panel onto <paramref name="target" /> at <paramref name="region" />.</summary>
    public void MovePanel(DockPanel panel, DockTabGroupNode target, DockRegion region)
    {
        // No-op / degenerate drops: dragging a panel onto its own group as a tab, or the only
        // panel of a group onto that same group's edge (which would just recreate the group).
        if (ReferenceEquals(FindGroup(panel), target) &&
            (region == DockRegion.Center || target.Panels.Count <= 1))
            return;

        if (FindGroup(panel) is { } source) source.Panels.Remove(panel);

        AddPanel(panel, target, region);
        Prune();
    }

    /// <summary>Dock a panel onto <paramref name="target" /> at <paramref name="region" />.</summary>
    public void AddPanel(DockPanel panel, DockTabGroupNode target, DockRegion region)
    {
        if (region == DockRegion.Center)
        {
            target.Panels.Add(panel);
            target.ActiveIndex = target.Panels.Count - 1;
            return;
        }

        var orientation = region is DockRegion.Left or DockRegion.Right
            ? DockOrientation.Horizontal
            : DockOrientation.Vertical;
        var before = region is DockRegion.Left or DockRegion.Top;
        var newGroup = new DockTabGroupNode(panel) { ActiveIndex = 0 };

        // Same-orientation parent: insert the new group as a sibling of the target.
        if (target.Parent is { } parent && parent.Orientation == orientation)
        {
            var siblingIndex = parent.Children.IndexOf(target);
            var targetWeight = parent.Weights[siblingIndex];
            parent.Insert(before ? siblingIndex : siblingIndex + 1, newGroup, targetWeight * 0.4f);
            return;
        }

        // Otherwise wrap the target in a fresh split. Capture the target's location BEFORE the
        // DockSplitNode constructor reparents it.
        var originalParent = target.Parent;
        var originalIndex = originalParent?.Children.IndexOf(target) ?? -1;
        var originalWeight = originalParent is not null ? originalParent.Weights[originalIndex] : 1f;

        var split = new DockSplitNode(orientation, before ? [newGroup, target] : [target, newGroup]);
        split.Weights[before ? 0 : 1] = 0.35f;
        split.Weights[before ? 1 : 0] = 0.65f;
        split.NormalizeWeights();

        if (originalParent is null)
        {
            Root = split;
            split.Parent = null;
        }
        else
        {
            originalParent.Children[originalIndex] = split;
            originalParent.Weights[originalIndex] = originalWeight;
            split.Parent = originalParent;
        }
    }

    public void ClosePanel(DockPanel panel)
    {
        if (FindGroup(panel) is not { } group) return;
        group.Panels.Remove(panel);
        Prune();
    }

    /// <summary>
    ///     Rebuild <see cref="Root" /> bottom-up: drop empty groups, dissolve single-child splits,
    ///     flatten a split nested directly inside a parent of the same orientation. Pure functional
    ///     walk — never mutates a list it is iterating.
    /// </summary>
    private void Prune()
    {
        Root = Normalize(Root) ?? new DockTabGroupNode();
        Root.Parent = null;
    }

    private static DockNode? Normalize(DockNode node)
    {
        switch (node)
        {
            case DockTabGroupNode group:
                return group.Panels.Count == 0 ? null : group;

            case DockSplitNode split:
            {
                var children = new List<DockNode>();
                var weights = new List<float>();

                for (var i = 0; i < split.Children.Count; i++)
                {
                    var normalized = Normalize(split.Children[i]);
                    if (normalized is null) continue;

                    var weight = i < split.Weights.Count ? split.Weights[i] : 1f;

                    if (normalized is DockSplitNode inner && inner.Orientation == split.Orientation)
                        for (var j = 0; j < inner.Children.Count; j++)
                        {
                            children.Add(inner.Children[j]);
                            weights.Add(inner.Weights[j] * weight);
                        }
                    else
                    {
                        children.Add(normalized);
                        weights.Add(weight);
                    }
                }

                if (children.Count == 0) return null;

                if (children.Count == 1)
                {
                    children[0].Parent = null;
                    return children[0];
                }

                split.Children.Clear();
                split.Children.AddRange(children);
                split.Weights.Clear();
                split.Weights.AddRange(weights);
                foreach (var child in split.Children) child.Parent = split;
                split.NormalizeWeights();
                return split;
            }

            default:
                return node;
        }
    }
}
