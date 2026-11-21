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

    internal record struct Named(string Name) : IComponentData;
}
