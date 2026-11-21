
namespace Rin.World.Tests.Ecs;

public class QueryTests
{
    [Test]
    public void VisitsOnlyEntitiesWithEveryQueriedComponent()
    {
        var world = new World();
        var expected = new List<Entity>();
        for (var i = 0; i < 3; i++)
        {
            var entity = world.Spawn();
            world.Add(entity, new Health(i));
            world.Add(entity, new Speed(i * 2f));
            expected.Add(entity);
        }

        world.Add(world.Spawn(), new Health(100));

        var visited = new List<Entity>();
        foreach (var chunk in world.Query<Health, Speed>())
            for (var i = 0; i < chunk.Length; i++)
            {
                visited.Add(chunk.Entities[i]);
                Assert.That(chunk.Components2[i].Value, Is.EqualTo(chunk.Components1[i].Value * 2f));
            }

        Assert.That(visited, Is.EquivalentTo(expected));
    }

    [Test]
    public void ChunkEntitiesLineUpWithTheComponentSpans()
    {
        var world = new World();
        for (var i = 0; i < 5; i++) world.Add(world.Spawn(), new Health(i));

        foreach (var (health, entities) in world.Query<Health>())
            for (var i = 0; i < health.Length; i++)
                Assert.That(world.Get<Health>(entities[i]), Is.EqualTo(health[i]));
    }

    [Test]
    public void WritesThroughTheSpansAreStored()
    {
        var world = new World();
        var entity = world.Spawn();
        world.Add(entity, new Health(1));
        world.Add(entity, new Speed(1f));

        foreach (var (health, speed) in world.Query<Health, Speed>())
            for (var i = 0; i < health.Length; i++)
                health[i].Value += (int)speed[i].Value;

        Assert.That(world.Get<Health>(entity).Value, Is.EqualTo(2));
    }

    [Test]
    public void FourComponentQueryMatchesOnlyFullEntities()
    {
        var world = new World();
        var full = world.Spawn();
        world.Add(full, new Health(1));
        world.Add(full, new Speed(1f));
        world.Add(full, new Armor(1));
        world.Add(full, new Ammo(1));
        var partial = world.Spawn();
        world.Add(partial, new Health(1));
        world.Add(partial, new Speed(1f));
        world.Add(partial, new Armor(1));

        Assert.That(world.Query<Health, Speed, Armor, Ammo>().EntityCount, Is.EqualTo(1));
        Assert.That(world.Query<Health, Speed, Armor>().EntityCount, Is.EqualTo(2));
    }

    [Test]
    public void ChunkIterationDoesNotAllocate()
    {
        var world = new World();
        for (var i = 0; i < 1000; i++)
        {
            var entity = world.Spawn();
            world.Add(entity, new Health(i));
            world.Add(entity, new Speed(1f));
            world.Add(entity, new Armor(i));
            world.Add(entity, new Ammo(i));
        }

        var one = world.Query<Health>();
        var four = world.Query<Health, Speed, Armor, Ammo>();
        Iterate(one, four);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 100; frame++) Iterate(one, four);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.That(allocated, Is.Zero);
    }

    [Test]
    public void WithVisitsOnlyEntitiesThatAlsoHaveTheComponentOrTag()
    {
        var world = new World();
        var stunned = Spawn(world, 1);
        world.AddTag<Stunned>(stunned);
        var armored = Spawn(world, 2);
        world.Add(armored, new Armor(1));
        Spawn(world, 3);

        Assert.That(Visit(world.Query<Health>().With<Stunned>()), Is.EqualTo(new[] { stunned }));
        Assert.That(Visit(world.Query<Health>().With<Armor>()), Is.EqualTo(new[] { armored }));
    }

    [Test]
    public void WithoutSkipsEntitiesWithTheComponentOrTag()
    {
        var world = new World();
        var stunned = Spawn(world, 1);
        world.AddTag<Stunned>(stunned);
        var plain = Spawn(world, 2);

        var awake = world.Query<Health>().Without<Stunned>();

        Assert.That(Visit(awake), Is.EqualTo(new[] { plain }));
        Assert.That(world.Query<Health>().EntityCount, Is.EqualTo(2), "a filter leaves the unfiltered query alone");
    }

    [Test]
    public void AFilteredQueryBuiltOnceSeesEntitiesAddedLater()
    {
        var world = new World();
        var slow = world.Query<Health>().Without<Speed>().With<Stunned>();

        var entity = Spawn(world, 1);
        world.AddTag<Stunned>(entity);
        Assert.That(slow.EntityCount, Is.EqualTo(1));

        world.Add(entity, new Speed(1f));
        Assert.That(slow.EntityCount, Is.Zero);
    }

    [Test]
    public void StructuralChangesInsideAQueryLoopThrow()
    {
        var world = new World();
        var entity = Spawn(world, 1);

        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (var _ in world.Query<Health>()) world.Add(entity, new Speed(1f));
        });
        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (var _ in world.Query<Health>()) world.Despawn(entity);
        });
        Assert.DoesNotThrow(() =>
        {
            foreach (var _ in world.Query<Health>()) world.Add(entity, new Health(2));
        }, "overwriting a component moves nothing");

        world.Add(entity, new Speed(1f));
        Assert.That(world.Has<Speed>(entity), Is.True);
    }

    [Test]
    public void QueryingInlineAndThroughAFilterDoesNotAllocateOnceWarmedUp()
    {
        var world = new World();
        for (var i = 0; i < 100; i++)
        {
            var entity = Spawn(world, i);
            if (i % 2 == 0) world.AddTag<Stunned>(entity);
        }

        var awake = world.Query<Health>().Without<Stunned>();
        var sum = Sum(world, awake);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 100; frame++) sum += Sum(world, awake);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.That(allocated, Is.Zero);
        Assert.That(sum, Is.Positive);
    }

    private static int Sum(World world, Query<Health> filtered)
    {
        var sum = 0;
        foreach (var (health, _) in world.Query<Health>())
            for (var i = 0; i < health.Length; i++)
                sum += health[i].Value;
        foreach (var (health, _) in filtered)
            for (var i = 0; i < health.Length; i++)
                sum += health[i].Value;
        return sum;
    }

    private static Entity Spawn(World world, int health)
    {
        var entity = world.Spawn();
        world.Add(entity, new Health(health));
        return entity;
    }

    private static List<Entity> Visit(Query<Health> query)
    {
        var visited = new List<Entity>();
        foreach (var chunk in query)
            for (var i = 0; i < chunk.Length; i++) visited.Add(chunk.Entities[i]);
        return visited;
    }

    private static void Iterate(Query<Health> one, Query<Health, Speed, Armor, Ammo> four)
    {
        foreach (var (health, _) in one)
            for (var i = 0; i < health.Length; i++)
                health[i].Value++;

        foreach (var chunk in four)
            for (var i = 0; i < chunk.Length; i++)
            {
                chunk.Components1[i].Value += chunk.Components3[i].Value + chunk.Components4[i].Value;
                _ = chunk.Entities[i];
            }
    }
}
