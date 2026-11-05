using Friflo.Engine.ECS;

namespace Rin.World.Ecs;

/// <summary>
///     The entity part of a <see cref="WorldSnapshot" />: the id allocator, then every non-empty archetype with its
///     component and tag sets, entity ids and one raw byte block per saved column, then the parent to children links.
/// </summary>
/// <remarks>
///     Entities are recreated straight into their archetype in the saved order, so query order after a restore matches
///     the order at save time. Derived columns and the hierarchy column are cleared rather than copied.
/// </remarks>
internal sealed partial class EcsStore
{
    private const int EndOfHierarchy = 0;

    public void Save(WorldSnapshot snapshot)
    {
        _ids.Save(snapshot);

        var archetypes = _store.Archetypes;
        var nonEmpty = 0;
        foreach (var archetype in archetypes)
            if (archetype.Count > 0) nonEmpty++;

        snapshot.Write(nonEmpty);
        foreach (var archetype in archetypes)
            if (archetype.Count > 0) SaveArchetype(archetype, snapshot);

        foreach (var archetype in archetypes)
            if (archetype.Count > 0 && archetype.ComponentTypes.Has<TreeNode>()) SaveChildren(archetype, snapshot);
        snapshot.Write(EndOfHierarchy);
    }

    /// <summary>
    ///     Replaces every entity with the snapshot's, without firing hooks.
    /// </summary>
    public void Restore(ref SnapshotReader reader)
    {
        DeleteAllEntities();
        _ids.Restore(ref reader);

        var archetypeCount = reader.Read<int>();
        for (var i = 0; i < archetypeCount; i++) RestoreArchetype(ref reader);

        RestoreChildren(ref reader);
    }

    private static void SaveArchetype(Archetype archetype, WorldSnapshot snapshot)
    {
        snapshot.Write(archetype.ComponentTypes);
        snapshot.Write(archetype.Tags);
        snapshot.Write(archetype.Count);
        snapshot.WriteSpan(archetype.EntityIds);

        foreach (var componentType in archetype.ComponentTypes)
        {
            var column = ComponentColumns.Get(componentType);
            if (column.IsSaved) column.Save(archetype, snapshot);
        }
    }

    private void SaveChildren(Archetype archetype, WorldSnapshot snapshot)
    {
        var ids = archetype.EntityIds;
        var trees = archetype.Components<TreeNode>();
        for (var i = 0; i < ids.Length; i++)
        {
            var children = trees[i].GetChildIds(_store);
            if (children.Length == 0) continue;
            snapshot.Write(ids[i]);
            snapshot.Write(children.Length);
            snapshot.WriteSpan(children);
        }
    }

    private void RestoreArchetype(ref SnapshotReader reader)
    {
        var componentTypes = reader.Read<ComponentTypes>();
        var tags = reader.Read<Tags>();
        var archetype = _store.GetArchetype(componentTypes, tags);
        var count = reader.Read<int>();
        var start = archetype.Count;
        for (var i = 0; i < count; i++) archetype.CreateEntity(reader.Read<int>());

        foreach (var componentType in archetype.ComponentTypes)
        {
            var column = ComponentColumns.Get(componentType);
            if (column.IsSaved) column.Restore(archetype, start, count, ref reader);
            else column.Clear(archetype, start, count);
        }
    }

    private void RestoreChildren(ref SnapshotReader reader)
    {
        for (var parentId = reader.Read<int>(); parentId != EndOfHierarchy; parentId = reader.Read<int>())
        {
            var parent = _store.GetEntityById(parentId);
            var count = reader.Read<int>();
            for (var i = 0; i < count; i++) parent.AddChild(_store.GetEntityById(reader.Read<int>()));
        }
    }

    private void DeleteAllEntities()
    {
        foreach (var archetype in _store.Archetypes)
        foreach (var id in archetype.EntityIds)
            _walk.Add(id);

        foreach (var id in _walk) _store.GetEntityById(id).DeleteEntity();
        _walk.Clear();
    }
}
