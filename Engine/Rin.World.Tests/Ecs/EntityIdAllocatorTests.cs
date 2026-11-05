namespace Rin.World.Tests.Ecs;

public class EntityIdAllocatorTests
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
