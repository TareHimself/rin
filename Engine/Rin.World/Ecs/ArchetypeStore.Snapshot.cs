using System.Runtime.InteropServices;

namespace Rin.World.Ecs;

/// <summary>
///     The entity part of a <see cref="WorldSnapshot" />: the id allocator, then every non-empty archetype with its key,
///     entity ids and one raw byte block per saved column, then each parent's children.
/// </summary>
/// <remarks>
///     Component ids are only valid within one schema, so a snapshot restores only into a store with the same schema.
///     Entities go straight back into their archetype in the saved order, so query order after a restore matches the
///     order at save time. Derived columns are cleared rather than copied.
/// </remarks>
public sealed partial class ArchetypeStore
{
    private const int EndOfHierarchy = 0;

    private ComponentId[] _keyBuffer = new ComponentId[16];

    public void Save(WorldSnapshot snapshot)
    {
        _ids.Save(snapshot);

        var nonEmpty = 0;
        foreach (var archetype in _archetypes)
            if (archetype.Count > 0) nonEmpty++;

        snapshot.Write(nonEmpty);
        foreach (var archetype in _archetypes)
            if (archetype.Count > 0) SaveArchetype(archetype, snapshot);

        foreach (var archetype in _archetypes)
        foreach (var id in archetype.EntityIds)
            SaveChildren(id, snapshot);
        snapshot.Write(EndOfHierarchy);
    }

    /// <summary>
    ///     Replaces every entity with the snapshot's, without firing hooks.
    /// </summary>
    public void Restore(WorldSnapshot snapshot)
    {
        var reader = snapshot.CreateReader();
        Restore(ref reader);
    }

    internal void Restore(ref SnapshotReader reader)
    {
        EnsureNotIterating();
        foreach (var archetype in _archetypes) archetype.Clear();
        _hierarchy.Clear();
        EntityCount = 0;
        _ids.Restore(ref reader);

        var archetypeCount = reader.Read<int>();
        for (var i = 0; i < archetypeCount; i++) RestoreArchetype(ref reader);

        RestoreChildren(ref reader);
    }

    private void SaveArchetype(Archetype archetype, WorldSnapshot snapshot)
    {
        snapshot.Write(archetype.Key.Length);
        snapshot.WriteSpan<ComponentId>(archetype.Key);
        snapshot.Write(archetype.Count);
        snapshot.WriteSpan(archetype.EntityIds);

        var columns = archetype.Columns;
        for (var i = 0; i < columns.Length; i++)
        {
            var info = Schema[archetype.ColumnIds[i]];
            if (info.IsDerived) continue;
            if (info.ContainsReferences)
                throw new InvalidOperationException(
                    $"{info.Name} holds managed references and cannot be saved in a snapshot. Keep managed data in a registry and store its id.");
            snapshot.WriteBytes(((ByteColumn)columns[i]).Bytes(0, archetype.Count));
        }
    }

    private void SaveChildren(int parent, WorldSnapshot snapshot)
    {
        var children = _hierarchy.ChildrenOf(parent);
        if (children.Length == 0) return;
        snapshot.Write(parent);
        snapshot.Write(children.Length);
        snapshot.WriteSpan(children);
    }

    private void RestoreArchetype(ref SnapshotReader reader)
    {
        var keyLength = reader.Read<int>();
        if (keyLength > _keyBuffer.Length) Array.Resize(ref _keyBuffer, keyLength);
        var key = _keyBuffer.AsSpan(0, keyLength);
        reader.ReadSpan(key);
        var archetype = GetOrCreateArchetype(key);

        var count = reader.Read<int>();
        var ids = MemoryMarshal.Cast<byte, int>(reader.ReadBytes(count * sizeof(int)));
        var start = archetype.AddRows(ids);
        for (var i = 0; i < count; i++)
        {
            EnsureLocation(ids[i]);
            _locations[ids[i]] = new EntityLocation(archetype, start + i);
        }

        EntityCount += count;

        var columns = archetype.Columns;
        for (var i = 0; i < columns.Length; i++)
        {
            if (Schema[archetype.ColumnIds[i]].IsDerived)
            {
                columns[i].ClearRows(start, count);
                continue;
            }

            var column = (ByteColumn)columns[i];
            reader.ReadBytes(count * column.Stride).CopyTo(column.Bytes(start, count));
        }
    }

    private void RestoreChildren(ref SnapshotReader reader)
    {
        for (var parent = reader.Read<int>(); parent != EndOfHierarchy; parent = reader.Read<int>())
        {
            var count = reader.Read<int>();
            for (var i = 0; i < count; i++) _hierarchy.SetParent(reader.Read<int>(), parent);
        }
    }
}
