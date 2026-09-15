namespace experiments.Docking.Model;

/// <summary>
///     A node in the dock tree. Either a <see cref="DockSplitNode" /> (an internal split) or a
///     <see cref="DockTabGroupNode" /> (a leaf holding one or more tabbed panels).
/// </summary>
public abstract class DockNode
{
    /// <summary>The split that contains this node, or null for the tree root.</summary>
    public DockSplitNode? Parent { get; internal set; }
}

/// <summary>
///     A leaf node: a stack of <see cref="DockPanel" />s shown as tabs, one active at a time.
/// </summary>
public sealed class DockTabGroupNode : DockNode
{
    public DockTabGroupNode(params DockPanel[] panels)
    {
        Panels.AddRange(panels);
    }

    public List<DockPanel> Panels { get; } = [];

    public int ActiveIndex
    {
        get;
        set => field = Panels.Count == 0 ? 0 : int.Clamp(value, 0, Panels.Count - 1);
    }

    public DockPanel? ActivePanel => Panels.Count == 0 ? null : Panels[int.Clamp(ActiveIndex, 0, Panels.Count - 1)];
}

/// <summary>
///     An internal node: an ordered set of children laid out along <see cref="Orientation" />,
///     each taking a fraction of the space given by the matching entry in <see cref="Weights" />
///     (weights are kept normalized to sum to 1).
/// </summary>
public sealed class DockSplitNode : DockNode
{
    public DockSplitNode(DockOrientation orientation, params DockNode[] children)
    {
        Orientation = orientation;
        foreach (var child in children)
        {
            child.Parent = this;
            Children.Add(child);
            Weights.Add(1f);
        }

        NormalizeWeights();
    }

    public DockOrientation Orientation { get; set; }
    public List<DockNode> Children { get; } = [];
    public List<float> Weights { get; } = [];

    public void Insert(int index, DockNode child, float weight)
    {
        child.Parent = this;
        Children.Insert(index, child);
        Weights.Insert(index, weight);
        NormalizeWeights();
    }

    public void RemoveAt(int index)
    {
        Children[index].Parent = null;
        Children.RemoveAt(index);
        Weights.RemoveAt(index);
        NormalizeWeights();
    }

    public void NormalizeWeights()
    {
        var total = 0f;
        foreach (var w in Weights) total += float.Max(w, 0.0001f);
        if (total <= 0f) return;
        for (var i = 0; i < Weights.Count; i++) Weights[i] = float.Max(Weights[i], 0.0001f) / total;
    }
}
