using Rin.World.Scheduling;

namespace Rin.World.Tests;

public class QueryChunkTests
{
    [Test]
    public void WriteStampsOnlyTheRowItWrites()
    {
        var world = new World();
        var written = Spawn(world, 1);
        var untouched = Spawn(world, 2);
        var changed = world.Query<Health>().Changed<Health>();
        ChangedRows(changed);
        world.Update(0f);

        foreach (var chunk in world.Query<Health>())
            for (var i = 0; i < chunk.Length; i++)
                if (chunk.Entities[i] == written) chunk.Write<Health>(i).Value = 10;

        Assert.That(ChangedRows(changed), Is.EqualTo(new[] { written }));
        Assert.That(world.Get<Health>(written).Value, Is.EqualTo(10));
        Assert.That(world.Get<Health>(untouched).Value, Is.EqualTo(2));
    }

    [Test]
    public void HasChangedComparesEachQueriedComponentOnItsOwn()
    {
        var world = new World();
        var entity = Spawn(world, 1);
        world.Add(entity, new Speed(1f));
        var changed = world.Query<Health, Speed>().Changed<Health>().Changed<Speed>();
        foreach (var _ in changed)
        {
        }

        world.Update(0f);
        world.Write<Speed>(entity).Value = 2f;

        var healthChanged = true;
        var speedChanged = false;
        foreach (var chunk in changed)
        {
            healthChanged = chunk.HasChanged<Health>(0);
            speedChanged = chunk.HasChanged<Speed>(0);
        }

        Assert.That(healthChanged, Is.False);
        Assert.That(speedChanged, Is.True);
    }

    [Test]
    public void ASystemDoesNotSeeItsOwnChunkWritesButSeesWritesMadeBetweenItsRuns()
    {
        var world = new World();
        var entity = Spawn(world, 1);
        Spawn(world, 2);
        var system = new HealthBumper(world.Query<Health>().Changed<Health>());
        world.AddSystem(FramePhase.Update, system);

        world.Update(0f);
        world.Update(0f);
        world.Write<Health>(entity).Value = 50;
        world.Update(0f);

        Assert.That(system.ChangedRowsPerRun, Is.EqualTo(new[] { 2, 0, 1 }));
    }

    [Test]
    public void HasChangedThrowsOnAQueryWithoutChanged()
    {
        var world = new World();
        Spawn(world, 1);

        var thrown = false;
        foreach (var chunk in world.Query<Health>())
            try
            {
                chunk.HasChanged<Health>(0);
            }
            catch (InvalidOperationException)
            {
                thrown = true;
            }

        Assert.That(thrown, Is.True);
    }

    [Test]
    public void WriteAndHasChangedThrowForAComponentTheQueryDoesNotHave()
    {
        var world = new World();
        var entity = Spawn(world, 1);
        world.Add(entity, new Armor(1));

        var thrown = 0;
        foreach (var chunk in world.Query<Health>().Changed<Health>())
        {
            try
            {
                chunk.Write<Armor>(0);
            }
            catch (InvalidOperationException)
            {
                thrown++;
            }

            try
            {
                chunk.HasChanged<Armor>(0);
            }
            catch (InvalidOperationException)
            {
                thrown++;
            }
        }

        Assert.That(thrown, Is.EqualTo(2));
    }

    [Test]
    public void WriteAndHasChangedDoNotAllocate()
    {
        var world = new World();
        for (var i = 0; i < 1000; i++)
        {
            var entity = Spawn(world, i);
            world.Add(entity, new Speed(1f));
            world.Add(entity, new Armor(i));
            world.Add(entity, new Ammo(i));
        }

        var one = world.Query<Health>().Changed<Health>();
        var four = world.Query<Health, Speed, Armor, Ammo>().Changed<Ammo>();
        WriteChanged(one, four);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 100; frame++) WriteChanged(one, four);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.That(allocated, Is.Zero);
    }

    private static void WriteChanged(Query<Health> one, Query<Health, Speed, Armor, Ammo> four)
    {
        foreach (var chunk in one)
            for (var i = 0; i < chunk.Length; i++)
                if (chunk.HasChanged<Health>(i)) chunk.Write<Health>(i).Value++;

        foreach (var chunk in four)
            for (var i = 0; i < chunk.Length; i++)
                if (chunk.HasChanged<Ammo>(i) || chunk.HasChanged<Speed>(i))
                    chunk.Write<Armor>(i).Value += chunk.Components4[i].Value;
    }

    private static Entity Spawn(World world, int health)
    {
        var entity = world.Spawn();
        world.Add(entity, new Health(health));
        return entity;
    }

    private static List<Entity> ChangedRows(Query<Health> changed)
    {
        var rows = new List<Entity>();
        foreach (var chunk in changed)
            for (var i = 0; i < chunk.Length; i++)
                if (chunk.HasChanged<Health>(i)) rows.Add(chunk.Entities[i]);
        return rows;
    }

    private sealed class HealthBumper(Query<Health> changed) : FrameSystem
    {
        public List<int> ChangedRowsPerRun { get; } = [];

        public override void Update(in FrameContext context)
        {
            var count = 0;
            foreach (var chunk in changed)
                for (var i = 0; i < chunk.Length; i++)
                {
                    if (chunk.HasChanged<Health>(i)) count++;
                    chunk.Write<Health>(i).Value++;
                }

            ChangedRowsPerRun.Add(count);
        }
    }
}
