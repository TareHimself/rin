namespace Rin.World.Ecs;

/// <summary>
///     The table of every entity with exactly the components in <see cref="Key" />: their ids plus one column per
///     component with a size. Rows are removed by moving the last row into the gap.
/// </summary>
internal sealed class Archetype
{
    private const int Absent = -1;
    private const int NoColumn = -2;
    private const int MinCapacity = 8;

    // Indexed by ComponentId: the column index, NoColumn for a tag, or Absent.
    private readonly int[] _slots;
    private readonly Column[] _columns;
    private readonly ComponentId[] _columnIds;
    private Archetype?[] _addEdges = [];
    private Archetype?[] _removeEdges = [];
    private int[] _entityIds = [];

    public Archetype(ComponentId[] key, ComponentSchema schema)
    {
        Key = key;
        _slots = new int[key.Length == 0 ? 0 : key[^1].Value + 1];
        _slots.AsSpan().Fill(Absent);

        var columns = new List<Column>();
        var columnIds = new List<ComponentId>();
        foreach (var id in key)
        {
            var info = schema[id];
            if (info.IsTag)
            {
                _slots[id.Value] = NoColumn;
                continue;
            }

            _slots[id.Value] = columns.Count;
            columns.Add(new Column(info.Size));
            columnIds.Add(id);
        }

        _columns = [.. columns];
        _columnIds = [.. columnIds];
    }

    /// <summary>
    ///     Sorted ascending.
    /// </summary>
    public ComponentId[] Key { get; }

    public int Count { get; private set; }

    public ReadOnlySpan<int> EntityIds => _entityIds.AsSpan(0, Count);

    public int[] EntityIdArray => _entityIds;

    public ReadOnlySpan<Column> Columns => _columns;

    public ReadOnlySpan<ComponentId> ColumnIds => _columnIds;

    public bool Has(ComponentId id) => (uint)id.Value < (uint)_slots.Length && _slots[id.Value] != Absent;

    /// <summary>
    ///     The column index of <paramref name="id" />, or -1 when it is absent or a tag.
    /// </summary>
    public int ColumnOf(ComponentId id) =>
        (uint)id.Value < (uint)_slots.Length && _slots[id.Value] >= 0 ? _slots[id.Value] : -1;

    public Span<T> Span<T>(ComponentId id) where T : unmanaged => _columns[ColumnOf(id)].Span<T>(Count);

    public bool HasAll(ReadOnlySpan<ComponentId> ids)
    {
        foreach (var id in ids)
            if (!Has(id)) return false;
        return true;
    }

    public bool HasAny(ReadOnlySpan<ComponentId> ids)
    {
        foreach (var id in ids)
            if (Has(id)) return true;
        return false;
    }

    public Archetype? AddEdge(ComponentId id) => id.Value < _addEdges.Length ? _addEdges[id.Value] : null;

    public Archetype? RemoveEdge(ComponentId id) => id.Value < _removeEdges.Length ? _removeEdges[id.Value] : null;

    public void SetAddEdge(ComponentId id, Archetype target) => SetEdge(ref _addEdges, id, target);

    public void SetRemoveEdge(ComponentId id, Archetype target) => SetEdge(ref _removeEdges, id, target);

    public int AddRow(int entityId)
    {
        EnsureCapacity(Count + 1);
        _entityIds[Count] = entityId;
        return Count++;
    }

    /// <summary>
    ///     Appends a row per id, with stale component bytes, and returns the first row.
    /// </summary>
    public int AddRows(ReadOnlySpan<int> entityIds)
    {
        var start = Count;
        EnsureCapacity(start + entityIds.Length);
        entityIds.CopyTo(_entityIds.AsSpan(start));
        Count += entityIds.Length;
        return start;
    }

    /// <summary>
    ///     Removes the row by moving the last row into it, and returns the id of the entity that moved, or 0 if none did.
    /// </summary>
    public int RemoveRow(int row)
    {
        var last = Count - 1;
        var moved = 0;
        if (row != last)
        {
            moved = _entityIds[last];
            _entityIds[row] = moved;
            foreach (var column in _columns) column.MoveRow(last, row);
        }

        Count = last;
        return moved;
    }

    public void Clear() => Count = 0;

    private void EnsureCapacity(int capacity)
    {
        if (capacity <= _entityIds.Length) return;
        var grown = int.Max(capacity, int.Max(MinCapacity, _entityIds.Length * 2));
        Array.Resize(ref _entityIds, grown);
        foreach (var column in _columns) column.Resize(grown);
    }

    private static void SetEdge(ref Archetype?[] edges, ComponentId id, Archetype target)
    {
        if (id.Value >= edges.Length) Array.Resize(ref edges, int.Max(id.Value + 1, edges.Length * 2));
        edges[id.Value] = target;
    }
}
