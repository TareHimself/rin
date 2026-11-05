using Rin.World.Scheduling;

namespace Rin.World.Tests;

public class CommandBufferTests
{
    [Test]
    public void NothingIsAppliedBeforePlayback()
    {
        var world = new World();
        var buffer = new CommandBuffer(world);
        var entity = buffer.Spawn();
        buffer.Add(entity, new Health(5));

        Assert.That(world.Has<Health>(entity), Is.False);

        buffer.Playback();

        Assert.That(world.Get<Health>(entity).Value, Is.EqualTo(5));
        Assert.That(buffer.Count, Is.Zero);
    }

    [Test]
    public void PlaybackFollowsRecordOrderAcrossComponentTypes()
    {
        var world = new World();
        var log = new List<string>();
        world.OnAdd<Health>((_, ref _) => log.Add("add Health"));
        world.OnRemove<Health>((_, ref _) => log.Add("remove Health"));
        world.OnAdd<Speed>((_, ref _) => log.Add("add Speed"));
        world.OnRemove<Speed>((_, ref _) => log.Add("remove Speed"));

        var buffer = new CommandBuffer(world);
        var entity = buffer.Spawn();
        buffer.Add(entity, new Speed(1f));
        buffer.Add(entity, new Health(1));
        buffer.Remove<Speed>(entity);
        buffer.Set(entity, new Health(2));
        buffer.Add(entity, new Speed(3f));
        buffer.Playback();

        Assert.That(log, Is.EqualTo(new[] { "add Speed", "add Health", "remove Speed", "add Speed" }));
        Assert.That(world.Get<Health>(entity).Value, Is.EqualTo(2));
        Assert.That(world.Get<Speed>(entity).Value, Is.EqualTo(3f));
    }

    [Test]
    public void RemoveThenAddKeepsTheComponentAndAddThenRemoveDropsIt()
    {
        var world = new World();
        var kept = world.Spawn();
        var dropped = world.Spawn();
        world.Add(kept, new Health(1));

        var buffer = new CommandBuffer(world);
        buffer.Remove<Health>(kept);
        buffer.Add(kept, new Health(2));
        buffer.Add(dropped, new Health(3));
        buffer.Remove<Health>(dropped);
        buffer.Playback();

        Assert.That(world.Get<Health>(kept).Value, Is.EqualTo(2));
        Assert.That(world.Has<Health>(dropped), Is.False);
    }

    [Test]
    public void HooksFireInRecordOrderWhenAnAddIsFollowedByADespawn()
    {
        var world = new World();
        var log = new List<string>();
        world.OnAdd((Entity _, ref Health _) => log.Add("add"));
        world.OnRemove((Entity _, ref Health _) => log.Add("remove"));

        var buffer = new CommandBuffer(world);
        var entity = buffer.Spawn();
        buffer.Add(entity, new Health(1));
        buffer.Despawn(entity);
        buffer.Playback();

        Assert.That(log, Is.EqualTo(new[] { "add", "remove" }));
        Assert.That(world.IsAlive(entity), Is.False);
    }

    [Test]
    public void TagCommandsFollowRecordOrder()
    {
        var world = new World();
        var added = world.Spawn();
        var removedAgain = world.Spawn();
        world.AddTag<Stunned>(removedAgain);

        var buffer = new CommandBuffer(world);
        buffer.AddTag<Stunned>(added);
        buffer.RemoveTag<Stunned>(removedAgain);
        buffer.AddTag<Stunned>(removedAgain);
        buffer.RemoveTag<Stunned>(removedAgain);
        Assert.That(world.HasTag<Stunned>(added), Is.False);

        buffer.Playback();

        Assert.That(world.HasTag<Stunned>(added), Is.True);
        Assert.That(world.HasTag<Stunned>(removedAgain), Is.False);
    }

    [Test]
    public void CommandsAfterADespawnAreSkipped()
    {
        var world = new World();
        var entity = world.Spawn();

        var buffer = new CommandBuffer(world);
        buffer.Despawn(entity);
        buffer.Add(entity, new Health(1));

        Assert.DoesNotThrow(buffer.Playback);
        Assert.That(world.IsAlive(entity), Is.False);
    }

    [Test]
    public void SpawnedIdsFollowRecordOrder()
    {
        var world = new World();
        var buffer = new CommandBuffer(world);

        var first = buffer.Spawn();
        var second = buffer.Spawn();
        var third = buffer.Spawn();

        Assert.That(new[] { first.Id, second.Id, third.Id }, Is.Ordered.Ascending);
    }

    [Test]
    public void WorldPlaysBackItsBufferAfterEachTick()
    {
        var world = new World { TickInterval = 0.125f };
        var spawner = new SpawnOnTick();
        world.AddSystem(TickPhase.Gameplay, spawner);
        var probe = new CountHealthOnFrame();
        world.AddSystem(FramePhase.Update, probe);

        world.Update(0.25f);

        Assert.That(spawner.HealthSeenAtTickStart, Is.EqualTo(new[] { 0, 1 }));
        Assert.That(probe.Count, Is.EqualTo(2));
    }

    [Test]
    public void WorldPlaysBackItsBufferAfterTheFramePhases()
    {
        var world = new World();
        var entity = world.Spawn();
        world.AddSystem(FramePhase.Update, new AddHealthOnFrame(entity));

        world.Update(0f);

        Assert.That(world.Has<Health>(entity), Is.True);
    }

    private sealed class SpawnOnTick : TickSystem
    {
        public readonly List<int> HealthSeenAtTickStart = [];

        public override void Tick(in TickContext context)
        {
            HealthSeenAtTickStart.Add(context.World.Query<Health>().EntityCount);
            var commands = context.World.Commands;
            commands.Add(commands.Spawn(), new Health(1));
        }
    }

    private sealed class CountHealthOnFrame : FrameSystem
    {
        public int Count;

        public override void Update(in FrameContext context) => Count = context.World.Query<Health>().EntityCount;
    }

    private sealed class AddHealthOnFrame(Entity entity) : FrameSystem
    {
        public override void Update(in FrameContext context) => context.World.Commands.Add(entity, new Health(1));
    }
}
