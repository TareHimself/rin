namespace Rin.World.Ecs;

/// <summary>
///     Each entity's parent and ordered children, by entity id. A child list keeps its array when it empties, so a
///     reused id or a restore fills it again without allocating.
/// </summary>
internal sealed class ChildStore
{
    private const int NoParent = 0;

    private int[] _parents = new int[64];
    private int[]?[] _children = new int[]?[64];
    private int[] _childCounts = new int[64];

    public int ParentOf(int id) => id < _parents.Length ? _parents[id] : NoParent;

    public ReadOnlySpan<int> ChildrenOf(int id) =>
        id < _childCounts.Length && _childCounts[id] > 0 ? _children[id].AsSpan(0, _childCounts[id]) : [];

    /// <summary>
    ///     Appends <paramref name="child" /> to the children of <paramref name="parent" />, moving it from its old parent.
    /// </summary>
    public void SetParent(int child, int parent)
    {
        ClearParent(child);
        EnsureSlot(int.Max(child, parent));

        ref var children = ref _children[parent];
        ref var count = ref _childCounts[parent];
        children ??= new int[4];
        if (count == children.Length) Array.Resize(ref children, count * 2);
        children[count++] = child;
        _parents[child] = parent;
    }

    public void ClearParent(int child)
    {
        var parent = ParentOf(child);
        if (parent == NoParent) return;

        var children = _children[parent]!;
        ref var count = ref _childCounts[parent];
        var index = Array.IndexOf(children, child, 0, count);
        children.AsSpan(index + 1, count - index - 1).CopyTo(children.AsSpan(index));
        count--;
        _parents[child] = NoParent;
    }

    /// <summary>
    ///     Detaches the entity from its parent and leaves its children as roots.
    /// </summary>
    public void Remove(int id)
    {
        ClearParent(id);
        foreach (var child in ChildrenOf(id)) _parents[child] = NoParent;
        if (id < _childCounts.Length) _childCounts[id] = 0;
    }

    public void Clear()
    {
        _parents.AsSpan().Clear();
        _childCounts.AsSpan().Clear();
    }

    private void EnsureSlot(int id)
    {
        if (id < _parents.Length) return;
        var size = int.Max(id + 1, _parents.Length * 2);
        Array.Resize(ref _parents, size);
        Array.Resize(ref _children, size);
        Array.Resize(ref _childCounts, size);
    }
}
