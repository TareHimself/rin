namespace Rin.World.Tests.Ecs;

public class EntityTableTests
{
    [Test]
    public void TheSameSpawnsAndDespawnsGiveTheSameHandlesInTwoWorlds()
    {
        Assert.That(RunSequence(new World()), Is.EqualTo(RunSequence(new World())));
    }

    [Test]
    public void ADespawnedIdIsReusedLastInFirstOutWithANewGeneration()
    {
        var world = new World();
        var first = world.Spawn();
        var second = world.Spawn();
        world.Despawn(first);
        world.Despawn(second);

        var reused = world.Spawn();

        Assert.That(reused.Id, Is.EqualTo(second.Id));
        Assert.That(reused.Generation, Is.EqualTo(second.Generation + 1));
    }

    [Test]
    public void SpawnsAfterARestoreGetTheHandlesTheyGotAfterTheSave()
    {
        var world = new World();
        RunSequence(world);
        var snapshot = new WorldSnapshot();
        world.SaveSnapshot(snapshot);
        var afterSave = SpawnAndDespawnSome(world);

        world.RestoreSnapshot(snapshot);
        var afterRestore = SpawnAndDespawnSome(world);

        Assert.That(afterRestore, Is.EqualTo(afterSave));
    }

    [Test]
    public void RestoreRevivesHandlesDespawnedAfterTheSaveAndKillsHandlesSpawnedAfterIt()
    {
        var world = new World();
        var kept = world.Spawn();
        var snapshot = new WorldSnapshot();
        world.SaveSnapshot(snapshot);
        world.Despawn(kept);
        var spawned = world.Spawn();

        world.RestoreSnapshot(snapshot);

        Assert.That(world.IsAlive(kept), Is.True);
        Assert.That(world.IsAlive(spawned), Is.False);
        Assert.That(world.EntityCount, Is.EqualTo(1));
    }

    [Test]
    public void NoSpawnedEntityIsTheDefaultHandleAndTheDefaultHandleIsNeverAlive()
    {
        var world = new World();
        var first = world.Spawn();
        world.Despawn(first);
        var spawned = new List<Entity> { first, world.Spawn(), world.Spawn() };

        Assert.That(spawned, Has.All.Matches<Entity>(entity => entity.IsValid));
        Assert.That(world.IsAlive(default), Is.False);
        Assert.That(world.GetParent(spawned[1]), Is.EqualTo(default(Entity)), "a root's parent is the default handle");
    }

    [Test]
    public void EveryLiveEntityFindsItsOwnComponentsThroughDespawnsReusedIdsAndARestore()
    {
        var world = new World();
        var live = new Dictionary<Entity, int>();
        for (var i = 0; i < 30; i++) SpawnWithHealth(world, live, i);
        foreach (var entity in live.Keys.Where((_, index) => index % 3 == 0).ToList())
        {
            world.Despawn(entity);
            live.Remove(entity);
        }

        for (var i = 30; i < 40; i++) SpawnWithHealth(world, live, i);
        var snapshot = new WorldSnapshot();
        world.SaveSnapshot(snapshot);
        foreach (var entity in live.Keys.Take(5).ToList()) world.Despawn(entity);
        world.RestoreSnapshot(snapshot);
        SpawnWithHealth(world, live, 40);

        Assert.That(world.EntityCount, Is.EqualTo(live.Count));
        foreach (var (entity, health) in live) Assert.That(world.Get<Health>(entity).Value, Is.EqualTo(health), $"{entity}");
    }

    private static void SpawnWithHealth(World world, Dictionary<Entity, int> live, int health)
    {
        var entity = world.Spawn();
        world.Add(entity, new Health(health));
        if (health % 2 == 0) world.Add(entity, new Stunned());
        live.Add(entity, health);
    }

    private static List<Entity> RunSequence(World world)
    {
        var handles = new List<Entity>();
        for (var round = 0; round < 4; round++)
        {
            for (var i = 0; i < 6; i++) handles.Add(world.Spawn());
            world.Despawn(handles[round * 3]);
            world.Despawn(handles[round * 3 + 2]);
        }

        handles.AddRange(SpawnAndDespawnSome(world));
        return handles;
    }

    private static List<Entity> SpawnAndDespawnSome(World world)
    {
        var handles = new List<Entity>();
        for (var i = 0; i < 5; i++) handles.Add(world.Spawn());
        world.Despawn(handles[1]);
        handles.Add(world.Spawn());
        return handles;
    }
}
