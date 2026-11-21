using Rin.World.Ecs;

namespace Rin.World.Tests.Ecs;

public class ArchetypeStoreTests
{
    [Test]
    public void ASpawnedEntityStartsInTheRootArchetype()
    {
        var store = new ArchetypeStore(new ComponentSchema());

        var entity = store.Spawn();

        Assert.That(store.LocationOf(entity).Archetype, Is.SameAs(store.Root));
        Assert.That(store.EntityCount, Is.EqualTo(1));
    }

    [Test]
    public void AddingAComponentMovesTheEntityToTheArchetypeWithIt()
    {
        var store = new ArchetypeStore(new ComponentSchema());
        var entity = store.Spawn();

        store.Add(entity, new Health(3));

        var archetype = store.LocationOf(entity).Archetype;
        Assert.That(archetype.Key, Is.EqualTo(new[] { store.Schema.IdOf<Health>() }));
        Assert.That(store.Root.Count, Is.Zero);
        Assert.That(store.Get<Health>(entity).Value, Is.EqualTo(3));
    }

    [Test]
    public void RemovingAComponentFollowsTheEdgeBackToTheArchetypeWithoutIt()
    {
        var store = new ArchetypeStore(new ComponentSchema());
        var entity = store.Spawn();
        store.Add(entity, new Health(1));
        var withHealth = store.LocationOf(entity).Archetype;
        store.Add(entity, new Speed(2f));

        Assert.That(store.Remove<Speed>(entity), Is.True);
        Assert.That(store.LocationOf(entity).Archetype, Is.SameAs(withHealth));
        Assert.That(store.Remove<Speed>(entity), Is.False);
    }

    [Test]
    public void AMoveKeepsTheValuesOfTheOtherComponents()
    {
        var store = new ArchetypeStore(new ComponentSchema());
        var entity = store.Spawn();
        store.Add(entity, new Health(7));
        store.Add(entity, new Speed(1.5f));

        store.Add(entity, new Armor(4));
        store.Remove<Speed>(entity);

        Assert.That(store.Get<Health>(entity).Value, Is.EqualTo(7));
        Assert.That(store.Get<Armor>(entity).Value, Is.EqualTo(4));
    }

    [Test]
    public void AddingTheSameComponentsInAnyOrderReachesOneArchetype()
    {
        var store = new ArchetypeStore(new ComponentSchema());
        var first = store.Spawn();
        var second = store.Spawn();

        store.Add(first, new Health(1));
        store.Add(first, new Speed(1f));
        store.Add(second, new Speed(2f));
        store.Add(second, new Health(2));

        Assert.That(store.LocationOf(second).Archetype, Is.SameAs(store.LocationOf(first).Archetype));
        Assert.That(store.Archetypes, Has.Count.EqualTo(4), "root, Health, Speed, Health and Speed");
    }

    [Test]
    public void RemovingARowMovesTheLastRowIntoTheGap()
    {
        var store = new ArchetypeStore(new ComponentSchema());
        var entities = new Entity[4];
        for (var i = 0; i < entities.Length; i++)
        {
            entities[i] = store.Spawn();
            store.Add(entities[i], new Health(i));
        }

        store.Remove<Health>(entities[1]);

        var archetype = store.LocationOf(entities[0]).Archetype;
        Assert.That(archetype.EntityIds.ToArray(), Is.EqualTo(new[] { entities[0].Id, entities[3].Id, entities[2].Id }));
        Assert.That(store.LocationOf(entities[3]).Row, Is.EqualTo(1));
        Assert.That(store.Get<Health>(entities[3]).Value, Is.EqualTo(3));
    }

    [Test]
    public void DespawningFillsTheGapAndFreesTheId()
    {
        var store = new ArchetypeStore(new ComponentSchema());
        var first = store.Spawn();
        var second = store.Spawn();
        store.Add(first, new Health(1));
        store.Add(second, new Health(2));

        store.Despawn(first);

        Assert.That(store.IsAlive(first), Is.False);
        Assert.That(store.EntityCount, Is.EqualTo(1));
        Assert.That(store.LocationOf(second).Row, Is.Zero);
        Assert.That(store.Get<Health>(second).Value, Is.EqualTo(2));
    }

    [Test]
    public void TheSameOperationsGiveTheSameRowsInTwoStores()
    {
        Assert.That(RowsAfterSomeChanges(new ArchetypeStore(new ComponentSchema())),
            Is.EqualTo(RowsAfterSomeChanges(new ArchetypeStore(new ComponentSchema()))));
    }

    [Test]
    public void ATagIsInTheKeyButHasNoColumn()
    {
        var store = new ArchetypeStore(new ComponentSchema());
        var entity = store.Spawn();
        store.Add(entity, new Health(1));

        store.AddTag<Stunned>(entity);

        var archetype = store.LocationOf(entity).Archetype;
        Assert.That(store.HasTag<Stunned>(entity), Is.True);
        Assert.That(archetype.Key, Has.Length.EqualTo(2));
        Assert.That(archetype.Columns.Length, Is.EqualTo(1));
        Assert.That(store.Get<Health>(entity).Value, Is.EqualTo(1));
    }

    [Test]
    public void ComponentsWithManagedReferencesSurviveMoves()
    {
        var store = new ArchetypeStore(new ComponentSchema());
        var first = store.Spawn();
        var second = store.Spawn();
        store.Add(first, new Named("first"));
        store.Add(second, new Named("second"));

        store.Add(first, new Health(1));
        store.Remove<Named>(second);

        Assert.That(store.Get<Named>(first).Name, Is.EqualTo("first"));
        Assert.That(store.Has<Named>(second), Is.False);
    }

    [Test]
    public void AddAndRemoveDoNotAllocateOnceWarmedUp()
    {
        var store = new ArchetypeStore(new ComponentSchema());
        var entities = new Entity[64];
        for (var i = 0; i < entities.Length; i++) entities[i] = store.Spawn();
        Churn(store, entities);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10; i++) Churn(store, entities);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.That(allocated, Is.Zero);
    }

    [Test]
    public void OnAddFiresOnlyForANewComponent()
    {
        var store = new ArchetypeStore(new ComponentSchema());
        var added = new List<int>();
        store.OnAdd((Entity _, ref Health health) => added.Add(health.Value));
        var entity = store.Spawn();

        store.Add(entity, new Health(1));
        store.Add(entity, new Health(2));

        Assert.That(added, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public void OnRemoveSeesTheComponentBeforeItIsRemoved()
    {
        var store = new ArchetypeStore(new ComponentSchema());
        var seen = new List<(int, bool)>();
        store.OnRemove((Entity entity, ref Health health) => seen.Add((health.Value, store.Has<Health>(entity))));
        var target = store.Spawn();
        store.Add(target, new Health(7));

        store.Remove<Health>(target);

        Assert.That(seen, Is.EqualTo(new[] { (7, true) }));
        Assert.That(store.Has<Health>(target), Is.False);
    }

    [Test]
    public void DespawnFiresRemoveHooksDepthFirstWithParentsBeforeChildren()
    {
        var store = new ArchetypeStore(new ComponentSchema());
        var removed = new List<Entity>();
        store.OnRemove((Entity entity, ref Health _) => removed.Add(entity));
        var root = SpawnWithHealth(store);
        var first = SpawnWithHealth(store);
        var grandchild = SpawnWithHealth(store);
        var second = SpawnWithHealth(store);
        store.SetParent(first, root);
        store.SetParent(grandchild, first);
        store.SetParent(second, root);

        store.Despawn(root);

        Assert.That(removed, Is.EqualTo(new[] { root, first, grandchild, second }));
        Assert.That(store.EntityCount, Is.Zero);
    }

    [Test]
    public void ARemoveHookMayDespawnAnotherEntity()
    {
        var store = new ArchetypeStore(new ComponentSchema());
        var removed = new List<Entity>();
        var other = SpawnWithHealth(store);
        var first = SpawnWithHealth(store);
        store.OnRemove((Entity entity, ref Health _) =>
        {
            removed.Add(entity);
            if (entity == first) store.Despawn(other);
        });

        store.Despawn(first);

        Assert.That(removed, Is.EqualTo(new[] { first, other }));
        Assert.That(store.IsAlive(other), Is.False);
        Assert.That(store.EntityCount, Is.Zero);
    }

    [Test]
    public void DespawnSkipsTheRemoveHookOfAComponentAnEarlierHookRemoved()
    {
        var store = new ArchetypeStore(new ComponentSchema());
        var speedHooks = 0;
        store.OnRemove((Entity entity, ref Health _) => store.Remove<Speed>(entity));
        store.OnRemove((Entity _, ref Speed _) => speedHooks++);
        var entity = SpawnWithHealth(store);
        store.Add(entity, new Speed(1f));

        store.Despawn(entity);

        Assert.That(speedHooks, Is.EqualTo(1), "only the Remove inside the Health hook");
        Assert.That(store.IsAlive(entity), Is.False);
    }

    [Test]
    public void SetParentAppendsTheChildAndMovesItFromItsOldParent()
    {
        var store = new ArchetypeStore(new ComponentSchema());
        var oldParent = store.Spawn();
        var parent = store.Spawn();
        var first = store.Spawn();
        var second = store.Spawn();
        store.SetParent(second, oldParent);

        store.SetParent(first, parent);
        store.SetParent(second, parent);

        Assert.That(Children(store, parent), Is.EqualTo(new[] { first, second }));
        Assert.That(store.ChildCount(oldParent), Is.Zero);
        Assert.That(store.GetParent(second), Is.EqualTo(parent));
    }

    [Test]
    public void ClearingAChildKeepsTheOrderOfTheOthers()
    {
        var store = new ArchetypeStore(new ComponentSchema());
        var parent = store.Spawn();
        var children = new[] { store.Spawn(), store.Spawn(), store.Spawn() };
        foreach (var child in children) store.SetParent(child, parent);

        store.ClearParent(children[1]);

        Assert.That(Children(store, parent), Is.EqualTo(new[] { children[0], children[2] }));
        Assert.That(store.GetParent(children[1]).IsValid, Is.False);
    }

    [Test]
    public void ParentingAnEntityUnderItselfOrItsDescendantThrows()
    {
        var store = new ArchetypeStore(new ComponentSchema());
        var root = store.Spawn();
        var child = store.Spawn();
        store.SetParent(child, root);

        Assert.Throws<InvalidOperationException>(() => store.SetParent(root, child));
        Assert.Throws<InvalidOperationException>(() => store.SetParent(root, root));
    }

    [Test]
    public void DespawningAChildRemovesItFromItsParent()
    {
        var store = new ArchetypeStore(new ComponentSchema());
        var parent = store.Spawn();
        var child = store.Spawn();
        store.SetParent(child, parent);

        store.Despawn(child);

        Assert.That(store.ChildCount(parent), Is.Zero);
        Assert.That(store.ChildCount(store.Spawn()), Is.Zero, "the reused id starts without children");
    }

    [Test]
    public void ParentingMovesNoRows()
    {
        var store = new ArchetypeStore(new ComponentSchema());
        var parent = SpawnWithHealth(store);
        var child = SpawnWithHealth(store);
        var before = store.LocationOf(child);

        store.SetParent(child, parent);

        Assert.That(store.LocationOf(child), Is.EqualTo(before));
    }

    [Test]
    public void ARestoreBringsBackComponentsTagsAndChildrenInTheSavedRowOrder()
    {
        var store = new ArchetypeStore(new ComponentSchema());
        var parent = SpawnWithHealth(store);
        var first = SpawnWithHealth(store);
        var second = SpawnWithHealth(store);
        store.Add(first, new Speed(2f));
        store.AddTag<Stunned>(second);
        store.SetParent(second, parent);
        store.SetParent(first, parent);
        store.Remove<Health>(parent);
        var rows = new[] { store.LocationOf(parent), store.LocationOf(first), store.LocationOf(second) };
        var snapshot = new WorldSnapshot();
        store.Save(snapshot);

        store.Despawn(parent);
        store.Add(store.Spawn(), new Armor(1));
        store.Restore(snapshot);

        Assert.That(new[] { store.LocationOf(parent), store.LocationOf(first), store.LocationOf(second) }, Is.EqualTo(rows));
        Assert.That(store.EntityCount, Is.EqualTo(3));
        Assert.That(store.Get<Speed>(first).Value, Is.EqualTo(2f));
        Assert.That(store.HasTag<Stunned>(second), Is.True);
        Assert.That(Children(store, parent), Is.EqualTo(new[] { second, first }));
    }

    [Test]
    public void ARestoreClearsDerivedComponentsAndFiresNoHooks()
    {
        var store = new ArchetypeStore(new ComponentSchema());
        var fired = 0;
        store.OnAdd((Entity _, ref Cached _) => fired++);
        store.OnRemove((Entity _, ref Cached _) => fired++);
        var entity = store.Spawn();
        store.Add(entity, new Cached(5));
        var snapshot = new WorldSnapshot();
        store.Save(snapshot);
        fired = 0;

        store.Restore(snapshot);

        Assert.That(store.Get<Cached>(entity).Value, Is.Zero);
        Assert.That(fired, Is.Zero);
    }

    [Test]
    public void ASnapshotRestoresIntoAnotherStoreWithTheSameSchema()
    {
        var schema = new ComponentSchema();
        var source = new ArchetypeStore(schema);
        var entity = SpawnWithHealth(source);
        source.AddTag<Stunned>(entity);
        var snapshot = new WorldSnapshot();
        source.Save(snapshot);

        var target = new ArchetypeStore(schema);
        target.Restore(snapshot);

        Assert.That(target.Get<Health>(entity).Value, Is.EqualTo(1));
        Assert.That(target.HasTag<Stunned>(entity), Is.True);
        Assert.That(target.Query<Health>().EntityCount, Is.EqualTo(1));
    }

    [Test]
    public void SavingAComponentWithManagedReferencesThrows()
    {
        var store = new ArchetypeStore(new ComponentSchema());
        store.Add(store.Spawn(), new Named("managed"));

        Assert.Throws<InvalidOperationException>(() => store.Save(new WorldSnapshot()));
    }

    [Test]
    public void SaveAndRestoreDoNotAllocateOnceWarmedUp()
    {
        var store = new ArchetypeStore(new ComponentSchema());
        for (var i = 0; i < 200; i++)
        {
            var entity = SpawnWithHealth(store);
            if (i % 2 == 0) store.Add(entity, new Speed(i));
            if (i % 10 != 0) store.SetParent(entity, new Entity(i / 10 * 10 + 1, 0));
        }

        var snapshot = new WorldSnapshot();
        SaveAndRestore(store, snapshot);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10; i++) SaveAndRestore(store, snapshot);

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.That(allocated, Is.Zero);
    }

    private static void SaveAndRestore(ArchetypeStore store, WorldSnapshot snapshot)
    {
        snapshot.Clear();
        store.Save(snapshot);
        store.Restore(snapshot);
    }

    private static void Churn(ArchetypeStore store, Entity[] entities)
    {
        foreach (var entity in entities)
        {
            store.Add(entity, new Health(1));
            store.Add(entity, new Speed(1f));
            store.AddTag<Stunned>(entity);
        }

        foreach (var entity in entities)
        {
            store.Remove<Health>(entity);
            store.RemoveTag<Stunned>(entity);
            store.Remove<Speed>(entity);
        }
    }

    private static List<(int Archetype, int Row)> RowsAfterSomeChanges(ArchetypeStore store)
    {
        var entities = new List<Entity>();
        for (var i = 0; i < 12; i++)
        {
            var entity = store.Spawn();
            entities.Add(entity);
            store.Add(entity, new Health(i));
            if (i % 2 == 0) store.Add(entity, new Speed(i));
            if (i % 3 == 0) store.AddTag<Stunned>(entity);
        }

        store.Remove<Health>(entities[4]);
        store.Despawn(entities[6]);
        store.Add(entities[1], new Armor(1));

        var rows = new List<(int, int)>();
        foreach (var entity in entities)
            if (store.IsAlive(entity)) rows.Add((store.LocationOf(entity).Archetype.Index, store.LocationOf(entity).Row));
        return rows;
    }

    private static Entity SpawnWithHealth(ArchetypeStore store)
    {
        var entity = store.Spawn();
        store.Add(entity, new Health(1));
        return entity;
    }

    private static List<Entity> Children(ArchetypeStore store, Entity parent)
    {
        var children = new List<Entity>();
        for (var i = 0; i < store.ChildCount(parent); i++) children.Add(store.ChildAt(parent, i));
        return children;
    }

    internal record struct Named(string Name) : IComponentData;

    internal record struct Cached(int Value) : IDerivedComponentData;
}
