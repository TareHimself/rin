namespace Rin.World.Tests;

public class WorldArchetypeTests
{
    [Test]
    public void AddingAndRemovingComponentsKeepsTheOtherValues()
    {
        var world = new World();
        var entity = world.Spawn();
        world.Add(entity, new Health(7));
        world.Add(entity, new Speed(1.5f));

        world.Add(entity, new Armor(4));
        world.AddTag<Stunned>(entity);
        world.Remove<Speed>(entity);

        Assert.That(world.Get<Health>(entity).Value, Is.EqualTo(7));
        Assert.That(world.Get<Armor>(entity).Value, Is.EqualTo(4));
        Assert.That(world.Has<Speed>(entity), Is.False);
    }

    [Test]
    public void RemovingAComponentMovesTheLastEntityOfItsArchetypeIntoTheGap()
    {
        var world = new World();
        var entities = new Entity[4];
        for (var i = 0; i < entities.Length; i++)
        {
            entities[i] = world.Spawn();
            world.Add(entities[i], new Health(i));
        }

        world.Remove<Health>(entities[1]);

        Assert.That(QueryOrder(world), Is.EqualTo(new[] { entities[0], entities[3], entities[2] }));
        Assert.That(world.Get<Health>(entities[3]).Value, Is.EqualTo(3));
    }

    [Test]
    public void EntitiesWithTheSameComponentsShareOneChunkWhateverTheAddOrder()
    {
        var world = new World();
        var first = world.Spawn();
        var second = world.Spawn();

        world.Add(first, new Health(1));
        world.Add(first, new Speed(1f));
        world.Add(second, new Speed(2f));
        world.Add(second, new Health(2));

        var chunks = 0;
        foreach (var chunk in world.Query<Health, Speed>())
        {
            chunks++;
            Assert.That(chunk.Length, Is.EqualTo(2));
        }

        Assert.That(chunks, Is.EqualTo(1));
    }

    [Test]
    public void TheSameChangesGiveTheSameQueryOrderInTwoWorlds()
    {
        Assert.That(OrderAfterSomeChanges(new World()), Is.EqualTo(OrderAfterSomeChanges(new World())));
    }

    [Test]
    public void AddingAndRemovingDoesNotAllocateOnceWarmedUp()
    {
        var world = new World();
        var entities = new Entity[64];
        for (var i = 0; i < entities.Length; i++) entities[i] = world.Spawn();
        Churn(world, entities);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10; i++) Churn(world, entities);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.That(allocated, Is.Zero);
    }

    private static void Churn(World world, Entity[] entities)
    {
        foreach (var entity in entities)
        {
            world.Add(entity, new Health(1));
            world.Add(entity, new Speed(1f));
            world.AddTag<Stunned>(entity);
        }

        foreach (var entity in entities)
        {
            world.Remove<Health>(entity);
            world.RemoveTag<Stunned>(entity);
            world.Remove<Speed>(entity);
        }
    }

    private static List<Entity> OrderAfterSomeChanges(World world)
    {
        var entities = new List<Entity>();
        for (var i = 0; i < 12; i++)
        {
            var entity = world.Spawn();
            entities.Add(entity);
            world.Add(entity, new Health(i));
            if (i % 2 == 0) world.Add(entity, new Speed(i));
            if (i % 3 == 0) world.AddTag<Stunned>(entity);
        }

        world.Remove<Health>(entities[4]);
        world.Despawn(entities[6]);
        world.Add(entities[1], new Armor(1));
        return QueryOrder(world);
    }

    private static List<Entity> QueryOrder(World world)
    {
        var order = new List<Entity>();
        foreach (var chunk in world.Query<Health>())
            for (var i = 0; i < chunk.Length; i++) order.Add(chunk.Entities[i]);
        return order;
    }
}
