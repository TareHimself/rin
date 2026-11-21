using Rin.Core.Shared.Math;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Rin.World.Scheduling;
using Rin.World.Transforms;

namespace Rin.World.Tests;

public class WorldSnapshotTests
{
    [Test]
    public void RestoreBringsBackComponentsTagsHierarchyTickAndRandom()
    {
        var world = new World();
        world.Random.Seed(99);
        var parent = world.Spawn();
        var first = world.Spawn();
        var second = world.Spawn();
        world.Add(parent, new Health(10));
        world.Add(first, new Health(20));
        world.Add(first, new Speed(1.5f));
        world.Add(second, new Stunned());
        world.SetPosition(second, new Vector3(1, 2, 3));
        world.SetParent(second, parent);
        world.SetParent(first, parent);
        Tick(world, 3);
        var snapshot = new WorldSnapshot();
        world.SaveSnapshot(snapshot);
        var draws = Draw(world.Random);

        world.Write<Health>(parent) = new Health(0);
        world.Remove<Speed>(first);
        world.Remove<Stunned>(second);
        world.Despawn(second);
        world.Add(world.Spawn(), new Armor(5));
        Tick(world, 2);

        world.RestoreSnapshot(snapshot);

        Assert.That(world.TickIndex, Is.EqualTo(3));
        Assert.That(snapshot.TickIndex, Is.EqualTo(3));
        Assert.That(world.EntityCount, Is.EqualTo(3));
        Assert.That(world.Get<Health>(parent).Value, Is.EqualTo(10));
        Assert.That(world.Get<Speed>(first).Value, Is.EqualTo(1.5f));
        Assert.That(world.Has<Stunned>(second), Is.True);
        Assert.That(world.Get<LocalTransform>(second).Position, Is.EqualTo(new Vector3(1, 2, 3)));
        Assert.That(world.GetParent(first), Is.EqualTo(parent));
        Assert.That(world.GetParent(second), Is.EqualTo(parent));
        Assert.That(ChildrenOf(world, parent), Is.EqualTo(new[] { second, first }));
        Assert.That(Draw(world.Random), Is.EqualTo(draws));
    }

    [Test]
    public void SavingARestoredWorldGivesTheSameBytes()
    {
        var world = new World();
        for (var i = 0; i < 20; i++)
        {
            var entity = world.Spawn();
            world.Add(entity, new Health(i));
            if (i % 2 == 0) world.Add(entity, new Speed(i));
            if (i % 3 == 0) world.Add(entity, new Stunned());
            if (i > 0 && i % 4 == 0) world.SetParent(entity, new Entity(i - 1, 0));
        }

        var saved = new WorldSnapshot();
        var resaved = new WorldSnapshot();
        world.SaveSnapshot(saved);
        world.Despawn(new Entity(3, 0));

        world.RestoreSnapshot(saved);
        world.SaveSnapshot(resaved);

        Assert.That(resaved.Bytes.ToArray(), Is.EqualTo(saved.Bytes.ToArray()));
        Assert.That(resaved.ComputeHash(), Is.EqualTo(saved.ComputeHash()));
    }

    [Test]
    public void RestoreKeepsTheSavedQueryOrder()
    {
        var world = new World();
        var entities = new List<Entity>();
        for (var i = 0; i < 6; i++)
        {
            var entity = world.Spawn();
            world.Add(entity, new Health(i));
            entities.Add(entity);
        }

        world.Remove<Health>(entities[1]);
        var saved = HealthOrder(world);
        var snapshot = new WorldSnapshot();
        world.SaveSnapshot(snapshot);
        world.Remove<Health>(entities[0]);
        world.Add(entities[1], new Health(9));

        world.RestoreSnapshot(snapshot);

        Assert.That(HealthOrder(world), Is.EqualTo(saved));
    }

    [Test]
    public void ASnapshotRestoresIntoAnotherWorldWithTheSameSchema()
    {
        var schema = ComponentSchema.Engine.Clone();
        var source = new World(schema);
        var entity = source.Spawn();
        source.Add(entity, new Health(4));
        source.Add(entity, new Stunned());
        var snapshot = new WorldSnapshot();
        source.SaveSnapshot(snapshot);

        var target = new World(schema);
        target.RestoreSnapshot(snapshot);

        Assert.That(target.Get<Health>(entity).Value, Is.EqualTo(4));
        Assert.That(target.Has<Stunned>(entity), Is.True);
        Assert.That(target.Query<Health>().EntityCount, Is.EqualTo(1));
    }

    [Test]
    public void ASnapshotRestoresIntoASchemaThatOnlyAppendsToTheSavedOne()
    {
        var schema = ComponentSchema.Engine.Clone();
        var source = new World(schema);
        var entity = source.Spawn();
        source.Add(entity, new Health(4));
        var snapshot = new WorldSnapshot();
        source.SaveSnapshot(snapshot);
        var extended = schema.Clone();
        extended.IdOf<Speed>();

        var target = new World(extended);
        target.RestoreSnapshot(snapshot);

        Assert.That(target.Get<Health>(entity).Value, Is.EqualTo(4));
    }

    [Test]
    public void ASnapshotRestoresIntoASchemaComposedWithMoreModules()
    {
        var combat = new ComponentModule(ComponentInfo.Of<Health>());
        var movement = new ComponentModule(ComponentInfo.Of<Speed>());
        var source = new World(ComponentSchema.Compose(ComponentModule.Engine, combat));
        var entity = source.Spawn();
        source.Add(entity, new Health(4));
        var snapshot = new WorldSnapshot();
        source.SaveSnapshot(snapshot);

        var target = new World(ComponentSchema.Compose(ComponentModule.Engine, combat, movement));
        target.RestoreSnapshot(snapshot);

        Assert.That(target.Get<Health>(entity).Value, Is.EqualTo(4));
    }

    [Test]
    public void RestoringIntoAWorldWhoseSchemaDiffersThrowsBeforeAnythingChanges()
    {
        var sourceSchema = ComponentSchema.Engine.Clone();
        sourceSchema.IdOf<Health>();
        sourceSchema.IdOf<Speed>();
        var source = new World(sourceSchema);
        source.Add(source.Spawn(), new Speed(1f));
        var snapshot = new WorldSnapshot();
        source.SaveSnapshot(snapshot);

        var reordered = ComponentSchema.Engine.Clone();
        reordered.IdOf<Speed>();
        reordered.IdOf<Health>();
        var log = new List<string>();
        var target = new World(reordered);
        target.AddRestoreListener(new LoggingListener("a", log, target));
        var kept = target.Spawn();
        target.Add(kept, new Health(9));

        Assert.Throws<ArgumentException>(() => target.RestoreSnapshot(snapshot));
        Assert.Throws<ArgumentException>(() => new World().RestoreSnapshot(snapshot), "the schema lacks the saved ids");
        Assert.That(log, Is.Empty);
        Assert.That(target.Get<Health>(kept).Value, Is.EqualTo(9));
        Assert.That(target.EntityCount, Is.EqualTo(1));
    }

    [Test]
    public void RestoringASnapshotWithAnUnsortedArchetypeKeyThrowsBeforeAnythingChanges()
    {
        var world = new World();
        var entity = world.Spawn();
        world.Add(entity, new Health(1));
        world.Add(entity, new Speed(1f));
        var snapshot = new WorldSnapshot();
        world.SaveSnapshot(snapshot);
        var health = world.Schema.IdOf<Health>().Value;
        var speed = world.Schema.IdOf<Speed>().Value;
        SwapArchetypeKey(snapshot, [2, health, speed, 1], [2, speed, health, 1]);
        world.Remove<Speed>(entity);

        Assert.Throws<ArgumentException>(() => world.RestoreSnapshot(snapshot));
        Assert.That(world.Has<Speed>(entity), Is.False);
        Assert.That(world.Get<Health>(entity).Value, Is.EqualTo(1));
    }

    [Test]
    public void RestoreDropsPendingCommands()
    {
        var world = new World();
        var entity = world.Spawn();
        var snapshot = new WorldSnapshot();
        world.SaveSnapshot(snapshot);
        world.Commands.Add(entity, new Health(1));

        world.RestoreSnapshot(snapshot);
        world.Commands.Playback();

        Assert.That(world.Has<Health>(entity), Is.False);
    }

    [Test]
    public void RestoreFiresNoHooks()
    {
        var world = new World();
        var fired = 0;
        world.OnAdd((Entity _, in Health _) => fired++);
        world.OnRemove((Entity _, in Health _) => fired++);
        world.Add(world.Spawn(), new Health(1));
        var snapshot = new WorldSnapshot();
        world.SaveSnapshot(snapshot);
        world.Add(world.Spawn(), new Health(2));
        fired = 0;

        world.RestoreSnapshot(snapshot);

        Assert.That(fired, Is.Zero);
    }

    [Test]
    public void RestoreCallsListenersBeforeAndAfterInRegistrationOrder()
    {
        var world = new World();
        var log = new List<string>();
        world.AddRestoreListener(new LoggingListener("a", log, world));
        world.AddRestoreListener(new LoggingListener("b", log, world));
        world.Spawn();
        var snapshot = new WorldSnapshot();
        world.SaveSnapshot(snapshot);
        world.Spawn();

        world.RestoreSnapshot(snapshot);

        Assert.That(log, Is.EqualTo(new[] { "a.before 2", "b.before 2", "a.after 1", "b.after 1" }));
    }

    [Test]
    public void RestoringDuringUpdateThrows()
    {
        var world = new World();
        var snapshot = new WorldSnapshot();
        world.SaveSnapshot(snapshot);
        world.AddSystem(TickPhase.Gameplay, new RestoreInTick(snapshot));

        Assert.Throws<InvalidOperationException>(() => world.Update(world.TickInterval));
    }

    [Test]
    public void RestoringAnEmptySnapshotThrows()
    {
        Assert.Throws<ArgumentException>(() => new World().RestoreSnapshot(new WorldSnapshot()));
    }

    [Test]
    public void SaveAndRestoreIntoAReusedSnapshotDoNotAllocate()
    {
        var world = new World();
        for (var i = 0; i < 500; i++)
        {
            var entity = world.Spawn();
            world.SetPosition(entity, new Vector3(i, 0, 0));
            world.Add(entity, new Health(i));
            if (i % 2 == 0) world.Add(entity, new Speed(i));
            if (i % 5 == 0) world.Add(entity, new Stunned());
            if (i % 10 != 0) world.SetParent(entity, new Entity(i / 10 * 10 + 1, 0));
        }

        var snapshot = new WorldSnapshot();
        SaveAndRestore(world, snapshot);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 20; i++) SaveAndRestore(world, snapshot);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        TestContext.Out.WriteLine($"{snapshot.Length} bytes per snapshot, {allocated} bytes allocated in 20 save and restore pairs");
        Assert.That(allocated, Is.Zero);
    }

    // Snapshots have no public writer, so this edits the saved bytes in place.
    private static void SwapArchetypeKey(WorldSnapshot snapshot, int[] saved, int[] replacement)
    {
        var bytes = MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in MemoryMarshal.GetReference(snapshot.Bytes)), snapshot.Length);
        var at = bytes.IndexOf(MemoryMarshal.AsBytes(saved.AsSpan()));
        Assert.That(at, Is.Not.Negative, "the saved key is in the snapshot");
        MemoryMarshal.AsBytes(replacement.AsSpan()).CopyTo(bytes[at..]);
    }

    private static List<Entity> HealthOrder(World world)
    {
        var order = new List<Entity>();
        foreach (var chunk in world.Query<Health>())
            for (var i = 0; i < chunk.Length; i++) order.Add(chunk.Entities[i]);
        return order;
    }

    private static void SaveAndRestore(World world, WorldSnapshot snapshot)
    {
        world.SaveSnapshot(snapshot);
        world.RestoreSnapshot(snapshot);
    }

    private static void Tick(World world, int ticks)
    {
        for (var i = 0; i < ticks; i++) world.Update(world.TickInterval);
    }

    private static ulong[] Draw(DeterministicRandom random) => [random.NextUInt64(), random.NextUInt64(), random.NextUInt64()];

    private static List<Entity> ChildrenOf(World world, Entity parent)
    {
        var children = new List<Entity>();
        foreach (var child in world.GetChildren(parent)) children.Add(child);
        return children;
    }

    private sealed class LoggingListener(string name, List<string> log, World world) : IWorldRestoreListener
    {
        public void BeforeRestore() => log.Add($"{name}.before {world.EntityCount}");

        public void AfterRestore() => log.Add($"{name}.after {world.EntityCount}");
    }

    private sealed class RestoreInTick(WorldSnapshot snapshot) : TickSystem
    {
        public override void Tick(in TickContext context) => context.World.RestoreSnapshot(snapshot);
    }
}
