using System.Runtime.InteropServices;

namespace Rin.World.Ecs;

/// <summary>
///     The entity part of a <see cref="WorldSnapshot" />: a layout hash per component id the snapshot uses, the entity
///     table, then every non-empty archetype with its key, entity ids and one raw byte block per saved column, then
///     each parent's children, ended by <see cref="EntityTable.NoId" />.
/// </summary>
/// <remarks>
///     Component ids are only valid within one schema, so <see cref="Validate" /> checks that the store's schema
///     describes every used id the same way before a restore changes anything. Entities go straight back into their
///     archetype in the saved order, so query order after a restore matches the order at save time.
///     <see cref="EcsNotSavedAttribute" /> columns are cleared rather than copied. Change ticks are not saved: a restore
///     stamps every row with a fresh tick, so every change query sees every restored row as changed.
/// </remarks>
internal sealed partial class ArchetypeStore
{
    private ComponentId[] _keyBuffer = new ComponentId[16];

    public void Save(WorldSnapshot snapshot)
    {
        SaveSchema(snapshot);
        _entities.Save(snapshot);

        var nonEmpty = 0;
        foreach (var archetype in _archetypes)
            if (archetype.Count > 0) nonEmpty++;

        snapshot.Write(nonEmpty);
        foreach (var archetype in _archetypes)
            if (archetype.Count > 0) SaveArchetype(archetype, snapshot);

        foreach (var archetype in _archetypes)
        foreach (var id in archetype.EntityIds)
            SaveChildren(id, snapshot);
        snapshot.Write(EntityTable.NoId);
    }

    /// <summary>
    ///     Replaces every entity with the snapshot's, without firing hooks.
    /// </summary>
    public void Restore(ref SnapshotReader reader)
    {
        Phase.EnsureCanRestore();
        SkipSchema(ref reader);
        foreach (var archetype in _archetypes) archetype.Clear();
        _hierarchy.Clear();
        EntityCount = 0;
        _entities.Restore(ref reader);
        Clock.Advance();

        var archetypeCount = reader.Read<int>();
        for (var i = 0; i < archetypeCount; i++) RestoreArchetype(ref reader);

        RestoreChildren(ref reader);
    }

    /// <summary>
    ///     Throws unless <see cref="Restore" /> can read the snapshot with this store's schema. Reads a copy of the
    ///     reader, so nothing changes.
    /// </summary>
    public void Validate(SnapshotReader reader)
    {
        var usedCount = ValidateSchema(ref reader);
        EntityTable.Skip(ref reader);
        var archetypeCount = reader.Read<int>();
        for (var i = 0; i < archetypeCount; i++) ValidateArchetype(ref reader, usedCount);
    }

    private void SaveSchema(WorldSnapshot snapshot)
    {
        var usedCount = 0;
        foreach (var archetype in _archetypes)
            if (archetype.Count > 0 && archetype.Key.Length > 0) usedCount = int.Max(usedCount, archetype.Key[^1].Value + 1);

        snapshot.Write(usedCount);
        for (var i = 0; i < usedCount; i++) snapshot.Write(Schema[new ComponentId(i)].LayoutHash());
    }

    private static void SkipSchema(ref SnapshotReader reader) => reader.ReadBytes(reader.Read<int>() * sizeof(ulong));

    private int ValidateSchema(ref SnapshotReader reader)
    {
        var usedCount = reader.Read<int>();
        if (usedCount > Schema.Count)
            throw new ArgumentException($"The snapshot uses {usedCount} component ids but the schema has only {Schema.Count}.");

        for (var i = 0; i < usedCount; i++)
        {
            var info = Schema[new ComponentId(i)];
            if (reader.Read<ulong>() != info.LayoutHash())
                throw new ArgumentException($"Component id {i} is {info.Name} in the schema but was saved as a different component.");
        }

        return usedCount;
    }

    private void ValidateArchetype(ref SnapshotReader reader, int usedCount)
    {
        var keyLength = reader.Read<int>();
        var key = MemoryMarshal.Cast<byte, ComponentId>(reader.ReadBytes(keyLength * sizeof(int)));
        for (var i = 0; i < key.Length; i++)
        {
            if ((uint)key[i].Value >= (uint)usedCount || (i > 0 && key[i].Value <= key[i - 1].Value))
                throw new ArgumentException("The snapshot has an archetype key that is not sorted, unique and within its schema.");
        }

        var count = reader.Read<int>();
        reader.ReadBytes(count * sizeof(int));
        foreach (var id in key)
        {
            var info = Schema[id];
            if (!info.IsTag && info.IsSaved) reader.ReadBytes(count * info.Size);
        }
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
            if (info.IsSaved) snapshot.WriteBytes(columns[i].Bytes(0, archetype.Count));
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
        for (var i = 0; i < count; i++) _entities.SetLocation(ids[i], new EntityLocation(archetype, start + i));

        EntityCount += count;

        var columns = archetype.Columns;
        for (var i = 0; i < columns.Length; i++)
        {
            var column = columns[i];
            if (Schema[archetype.ColumnIds[i]].IsSaved) reader.ReadBytes(count * column.Stride).CopyTo(column.Bytes(start, count));
            else column.ClearRows(start, count);
            column.StampRows(start, count, Clock.Current);
        }
    }

    private void RestoreChildren(ref SnapshotReader reader)
    {
        for (var parent = reader.Read<int>(); parent != EntityTable.NoId; parent = reader.Read<int>())
        {
            var count = reader.Read<int>();
            for (var i = 0; i < count; i++) _hierarchy.SetParent(reader.Read<int>(), parent);
        }
    }
}
