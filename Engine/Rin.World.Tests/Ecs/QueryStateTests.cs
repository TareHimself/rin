using Rin.World.Ecs;

namespace Rin.World.Tests.Ecs;

public class QueryStateTests
{
    [Test]
    public void AQueryMatchesOnlyArchetypesWithEveryQueriedComponent()
    {
        var store = new ArchetypeStore(new ComponentSchema());
        Spawn(store, new Health(1));
        var both = Spawn(store, new Health(2));
        store.Add(both, new Speed(1f));
        store.Add(Spawn(store, new Speed(2f)), new Armor(1));

        var query = store.Query<Health, Speed>();

        Assert.That(query.Matched, Is.EqualTo(new[] { store.LocationOf(both).Archetype }));
        Assert.That(query.EntityCount, Is.EqualTo(1));
    }

    [Test]
    public void TheSameUnfilteredQueryIsReturnedEveryCall()
    {
        var store = new ArchetypeStore(new ComponentSchema());

        Assert.That(store.Query<Health, Speed>(), Is.SameAs(store.Query<Health, Speed>()));
        Assert.That(store.Query<Speed, Health>(), Is.Not.SameAs(store.Query<Health, Speed>()));
    }

    [Test]
    public void AQueryPicksUpArchetypesCreatedAfterItsFirstUse()
    {
        var store = new ArchetypeStore(new ComponentSchema());
        var query = store.Query<Health>();
        Spawn(store, new Health(1));
        Assert.That(query.EntityCount, Is.EqualTo(1));

        store.AddTag<Stunned>(Spawn(store, new Health(2)));

        Assert.That(query.EntityCount, Is.EqualTo(2));
        Assert.That(query.Matched, Has.Count.EqualTo(2));
    }

    [Test]
    public void MatchedArchetypesStayInCreationOrder()
    {
        var store = new ArchetypeStore(new ComponentSchema());
        var tagged = Spawn(store, new Health(1));
        store.AddTag<Stunned>(tagged);
        var plain = Spawn(store, new Health(2));

        var matched = store.Query<Health>().Matched;

        Assert.That(matched[0].Index, Is.LessThan(matched[1].Index));
        Assert.That(matched, Does.Contain(store.LocationOf(plain).Archetype));
    }

    [Test]
    public void WithRequiresAndWithoutExcludesAComponentOrTag()
    {
        var store = new ArchetypeStore(new ComponentSchema());
        var stunned = Spawn(store, new Health(1));
        store.AddTag<Stunned>(stunned);
        var fast = Spawn(store, new Health(2));
        store.Add(fast, new Speed(1f));

        var stunnedOnly = store.Query<Health>().With(store.Schema.IdOf<Stunned>());
        var slow = store.Query<Health>().Without(store.Schema.IdOf<Speed>());

        Assert.That(stunnedOnly.Matched, Is.EqualTo(new[] { store.LocationOf(stunned).Archetype }));
        Assert.That(slow.Matched, Does.Not.Contain(store.LocationOf(fast).Archetype));
        Assert.That(slow.EntityCount, Is.EqualTo(1));
        Assert.That(store.Query<Health>().EntityCount, Is.EqualTo(2), "filters leave the cached query alone");
    }

    [Test]
    public void StructuralChangesThrowWhileIterating()
    {
        var store = new ArchetypeStore(new ComponentSchema());
        var entity = Spawn(store, new Health(1));

        store.BeginIteration();
        Assert.Throws<InvalidOperationException>(() => store.Add(entity, new Speed(1f)));
        Assert.Throws<InvalidOperationException>(() => store.Remove<Health>(entity));
        Assert.Throws<InvalidOperationException>(() => store.Despawn(entity));
        Assert.DoesNotThrow(() => store.Add(entity, new Health(2)), "overwriting moves nothing");
        store.EndIteration();

        Assert.DoesNotThrow(() => store.Add(entity, new Speed(1f)));
    }

    [Test]
    public void GettingACachedQueryDoesNotAllocate()
    {
        var store = new ArchetypeStore(new ComponentSchema());
        Spawn(store, new Health(1));
        var count = store.Query<Health>().EntityCount + store.Query<Health, Speed, Armor, Ammo>().EntityCount;

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 100; i++)
            count += store.Query<Health>().EntityCount + store.Query<Health, Speed, Armor, Ammo>().EntityCount;
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.That(allocated, Is.Zero);
        Assert.That(count, Is.EqualTo(101));
    }

    private static Entity Spawn<T>(ArchetypeStore store, T component) where T : struct
    {
        var entity = store.Spawn();
        store.Add(entity, component);
        return entity;
    }
}
