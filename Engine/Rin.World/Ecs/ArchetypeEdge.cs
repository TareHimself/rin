namespace Rin.World.Ecs;

/// <summary>
///     Where adding or removing one component leads from <see cref="Source" />, with the columns a moving row copies,
///     worked out once when the edge is created so a move does no schema or column lookups.
/// </summary>
internal sealed class ArchetypeEdge
{
    private readonly ColumnCopy[] _copies;

    public ArchetypeEdge(Archetype source, Archetype target)
    {
        Source = source;
        Target = target;
        _copies = SharedColumns(source, target);
    }

    public Archetype Source { get; }

    public Archetype Target { get; }

    /// <summary>
    ///     Copies every column the two archetypes share from <paramref name="sourceRow" /> into
    ///     <paramref name="targetRow" />.
    /// </summary>
    public void CopyRow(int sourceRow, int targetRow)
    {
        foreach (var copy in _copies) copy.Source.CopyRowTo(sourceRow, copy.Target, targetRow);
    }

    private static ColumnCopy[] SharedColumns(Archetype source, Archetype target)
    {
        var copies = new List<ColumnCopy>();
        var ids = source.ColumnIds;
        for (var i = 0; i < ids.Length; i++)
        {
            var targetColumn = target.ColumnOf(ids[i]);
            if (targetColumn >= 0) copies.Add(new ColumnCopy(source.Columns[i], target.Columns[targetColumn]));
        }

        return [.. copies];
    }

    /// <summary>
    ///     A column pair, held directly because an archetype's columns never change after it is built.
    /// </summary>
    private readonly record struct ColumnCopy(Column Source, Column Target);
}
