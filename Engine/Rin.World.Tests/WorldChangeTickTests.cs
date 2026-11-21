namespace Rin.World.Tests;

public class WorldChangeTickTests
{
    [Test]
    public void AddingAComponentStampsItWithTheCurrentTick()
    {
        var world = new World();
        var entity = world.Spawn();
        world.Add(entity, new Health(1));

        Assert.That(world.HasChangedSince<Health>(entity, world.ChangeTick - 1), Is.True);
        Assert.That(world.HasChangedSince<Health>(entity, world.ChangeTick), Is.False);
    }

    [Test]
    public void SetAndOverwritingAddStampTheRowAgain()
    {
        var world = new World();
        var entity = world.Spawn();
        world.Add(entity, new Health(1));
        var added = world.ChangeTick;

        AdvanceClock(world);
        Assert.That(world.HasChangedSince<Health>(entity, added), Is.False);
        world.Set(entity, new Health(2));
        Assert.That(world.HasChangedSince<Health>(entity, added), Is.True);

        var set = world.ChangeTick;
        AdvanceClock(world);
        world.Add(entity, new Health(3));
        Assert.That(world.HasChangedSince<Health>(entity, set), Is.True);
    }

    [Test]
    public void AMoveToAnotherArchetypeKeepsTheTicksOfTheComponentsItCarries()
    {
        var world = new World();
        var entity = world.Spawn();
        world.Add(entity, new Health(1));
        var added = world.ChangeTick;
        AdvanceClock(world);

        world.Add(entity, new Speed(1f));
        world.Remove<Speed>(entity);

        Assert.That(world.HasChangedSince<Health>(entity, added), Is.False);
        Assert.That(world.HasChangedSince<Health>(entity, added - 1), Is.True);
    }

    [Test]
    public void TheRowThatFillsARemovedRowKeepsItsTick()
    {
        var world = new World();
        var removed = world.Spawn();
        world.Add(removed, new Health(1));
        AdvanceClock(world);
        var last = world.Spawn();
        world.Add(last, new Health(2));
        var lastAdded = world.ChangeTick;
        AdvanceClock(world);

        world.Despawn(removed);

        Assert.That(world.HasChangedSince<Health>(last, lastAdded), Is.False);
        Assert.That(world.HasChangedSince<Health>(last, lastAdded - 1), Is.True);
    }

    [Test]
    public void CommandPlaybackStampsTheRowsItWrites()
    {
        var world = new World();
        var entity = world.Spawn();
        world.Add(entity, new Health(1));
        var before = world.ChangeTick;

        world.Commands.Set(entity, new Health(2));
        world.Update(0f);

        Assert.That(world.HasChangedSince<Health>(entity, before), Is.True);
    }

    [Test]
    public void ARestoreStampsEveryRestoredRowWithAFreshTick()
    {
        var world = new World();
        var entity = world.Spawn();
        world.Add(entity, new Health(1));
        var snapshot = new WorldSnapshot();
        world.SaveSnapshot(snapshot);
        AdvanceClock(world);
        var beforeRestore = world.ChangeTick;

        world.RestoreSnapshot(snapshot);

        Assert.That(world.ChangeTick, Is.GreaterThan(beforeRestore));
        Assert.That(world.HasChangedSince<Health>(entity, beforeRestore), Is.True);
    }

    [Test]
    public void ATagHasNoChangeTicks()
    {
        var world = new World();
        var entity = world.Spawn();
        world.Add(entity, new Stunned());

        Assert.Throws<InvalidOperationException>(() => world.HasChangedSince<Stunned>(entity, 0));
    }

    // Every world runs transform propagation, so an update always advances the clock.
    private static void AdvanceClock(World world) => world.Update(0f);
}
