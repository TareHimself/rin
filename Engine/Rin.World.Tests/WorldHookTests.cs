
namespace Rin.World.Tests;

public class WorldHookTests
{
    [Test]
    public void OnAddFiresOnlyForANewComponent()
    {
        var world = new World();
        var added = new List<int>();
        world.OnAdd((Entity _, ref Health health) => added.Add(health.Value));
        var entity = world.Spawn();

        world.Add(entity, new Health(1));
        world.Add(entity, new Health(2));

        Assert.That(added, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public void OnRemoveSeesTheValueBeforeItIsRemoved()
    {
        var world = new World();
        var removed = new List<(Entity, int, bool)>();
        world.OnRemove((Entity entity, ref Health health) => removed.Add((entity, health.Value, world.Has<Health>(entity))));
        var target = world.Spawn();
        world.Add(target, new Health(7));

        world.Remove<Health>(target);
        world.Remove<Health>(target);

        Assert.That(removed, Is.EqualTo(new[] { (target, 7, true) }));
    }

    [Test]
    public void DespawnFiresOnRemoveForEveryComponentOfEveryDescendant()
    {
        var world = new World();
        var removed = new List<Entity>();
        world.OnRemove((Entity entity, ref Health _) => removed.Add(entity));
        world.OnRemove((Entity entity, ref Speed _) => removed.Add(entity));
        var parent = world.Spawn();
        var first = world.Spawn();
        var firstChild = world.Spawn();
        var second = world.Spawn();
        world.Add(parent, new Health(1));
        world.Add(parent, new Speed(1f));
        foreach (var entity in new[] { first, firstChild, second }) world.Add(entity, new Health(1));
        world.SetParent(first, parent);
        world.SetParent(firstChild, first);
        world.SetParent(second, parent);

        world.Despawn(parent);

        Assert.That(removed, Is.EqualTo(new[] { parent, parent, first, firstChild, second }), "depth first, parent before children");
    }

    [Test]
    public void HooksForOtherTypesDoNotFire()
    {
        var world = new World();
        var fired = 0;
        world.OnAdd((Entity _, ref Speed _) => fired++);

        world.Add(world.Spawn(), new Health(1));

        Assert.That(fired, Is.Zero);
    }

    [Test]
    public void DespawnSkipsTheRemoveHookOfAComponentAnEarlierHookRemoved()
    {
        var world = new World();
        var speedPresentInHook = new List<bool>();
        world.OnRemove((Entity entity, ref Health _) => world.Remove<Speed>(entity));
        world.OnRemove((Entity entity, ref Speed _) => speedPresentInHook.Add(world.Has<Speed>(entity)));
        var entity = world.Spawn();
        world.Add(entity, new Health(1));
        world.Add(entity, new Speed(1f));

        world.Despawn(entity);

        Assert.That(speedPresentInHook, Is.Not.Empty.And.All.True);
        Assert.That(world.IsAlive(entity), Is.False);
    }

    [Test]
    public void FiringHooksDoesNotAllocate()
    {
        var world = new World();
        var counter = new Counter();
        world.OnAdd((Entity _, ref Health _) => counter.Value++);
        world.OnRemove((Entity _, ref Health _) => counter.Value++);
        var entity = world.Spawn();
        world.Add(entity, new Health(1));
        world.Remove<Health>(entity);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 100; i++)
        {
            world.Add(entity, new Health(i));
            world.Remove<Health>(entity);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.That(allocated, Is.Zero);
        Assert.That(counter.Value, Is.EqualTo(202));
    }

    private sealed class Counter
    {
        public int Value;
    }
}
