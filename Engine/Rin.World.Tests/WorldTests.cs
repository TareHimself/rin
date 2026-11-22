
namespace Rin.World.Tests;

public class WorldTests
{
    [Test]
    public void DefaultEntityIsInvalidAndNotAlive()
    {
        var world = new World();

        Assert.That(default(Entity).IsValid, Is.False);
        Assert.That(world.IsAlive(default), Is.False);
    }

    [Test]
    public void StaleHandleDoesNotMatchARecycledId()
    {
        var world = new World();
        var first = world.Spawn();
        world.Despawn(first);
        var second = world.Spawn();

        Assert.That(second.Id, Is.EqualTo(first.Id), "sanity check: the id is recycled");
        Assert.That(second, Is.Not.EqualTo(first));
        Assert.That(world.IsAlive(first), Is.False);
        Assert.That(world.IsAlive(second), Is.True);
    }

    [Test]
    public void CollectEntitiesListsLiveEntitiesInIdOrderAndReplacesTheList()
    {
        var world = new World();
        var first = world.Spawn();
        var second = world.Spawn();
        var third = world.Spawn();
        world.Despawn(second);
        var entities = new List<Entity> { default };

        world.CollectEntities(entities);

        Assert.That(entities, Is.EqualTo(new[] { first, third }));
    }

    [Test]
    public void AddOverwritesAndSetRequiresTheComponent()
    {
        var world = new World();
        var entity = world.Spawn();

        Assert.Throws<InvalidOperationException>(() => world.Set(entity, new Health(1)));

        world.Add(entity, new Health(1));
        world.Add(entity, new Health(2));
        Assert.That(world.Get<Health>(entity).Value, Is.EqualTo(2));

        world.Set(entity, new Health(3));
        Assert.That(world.Get<Health>(entity).Value, Is.EqualTo(3));
    }

    [Test]
    public void TagsAreAddedReadAndRemovedAsComponents()
    {
        var world = new World();
        var entity = world.Spawn();

        world.Add(entity, new Stunned());
        Assert.That(world.Has<Stunned>(entity), Is.True);
        Assert.DoesNotThrow(() => world.Get<Stunned>(entity));

        world.Remove<Stunned>(entity);
        Assert.That(world.Has<Stunned>(entity), Is.False);
    }

    [Test]
    public void UsingADeadEntityThrows()
    {
        var world = new World();
        var entity = world.Spawn();
        world.Despawn(entity);

        Assert.Throws<InvalidOperationException>(() => world.Add(entity, new Health(1)));
    }
}
