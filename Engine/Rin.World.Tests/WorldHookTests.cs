
namespace Rin.World.Tests;

public class WorldHookTests
{
    [Test]
    public void OnAddFiresOnlyForANewComponent()
    {
        var world = new World();
        var added = new List<int>();
        world.OnAdd((Entity _, in Health health) => added.Add(health.Value));
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
        world.OnRemove((Entity entity, in Health health) => removed.Add((entity, health.Value, world.Has<Health>(entity))));
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
        world.OnRemove((Entity entity, in Health _) => removed.Add(entity));
        world.OnRemove((Entity entity, in Speed _) => removed.Add(entity));
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
        world.OnAdd((Entity _, in Speed _) => fired++);

        world.Add(world.Spawn(), new Health(1));

        Assert.That(fired, Is.Zero);
    }

    [Test]
    public void DespawnSkipsTheRemoveHookOfAComponentAnEarlierHookRemoved()
    {
        var world = new World();
        var speedPresentInHook = new List<bool>();
        world.OnRemove((Entity entity, in Health _) => world.Remove<Speed>(entity));
        world.OnRemove((Entity entity, in Speed _) => speedPresentInHook.Add(world.Has<Speed>(entity)));
        var entity = world.Spawn();
        world.Add(entity, new Health(1));
        world.Add(entity, new Speed(1f));

        world.Despawn(entity);

        Assert.That(speedPresentInHook, Is.Not.Empty.And.All.True);
        Assert.That(world.IsAlive(entity), Is.False);
    }

    [Test]
    public void ARemoveHookMayDespawnAnotherEntity()
    {
        var world = new World();
        var removed = new List<Entity>();
        var other = world.Spawn();
        var first = world.Spawn();
        world.Add(other, new Health(1));
        world.Add(first, new Health(2));
        world.OnRemove((Entity entity, in Health _) =>
        {
            removed.Add(entity);
            if (entity == first) world.Despawn(other);
        });

        world.Despawn(first);

        Assert.That(removed, Is.EqualTo(new[] { first, other }));
        Assert.That(world.EntityCount, Is.Zero);
    }

    [Test]
    public void FiringHooksDoesNotAllocate()
    {
        var world = new World();
        var counter = new Counter();
        world.OnAdd((Entity _, in Health _) => counter.Value++);
        world.OnRemove((Entity _, in Health _) => counter.Value++);
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

    [Test]
    public void ARemoveHookThatRemovesItsOwnComponentDoesNotFireItselfAgain()
    {
        var world = new World();
        var nested = new List<bool>();
        world.OnRemove((Entity entity, in Health _) => nested.Add(world.Remove<Health>(entity)));
        var target = world.Spawn();
        world.Add(target, new Health(1));

        var removed = world.Remove<Health>(target);

        Assert.That(removed, Is.True);
        Assert.That(nested, Is.EqualTo(new[] { false }));
        Assert.That(world.Has<Health>(target), Is.False);
    }

    [Test]
    public void ARemoveHookThatDespawnsItsOwnEntityDoesNotFireItselfAgain()
    {
        var world = new World();
        var fired = 0;
        world.OnRemove((Entity entity, in Health _) =>
        {
            fired++;
            world.Despawn(entity);
        });
        var target = world.Spawn();
        world.Add(target, new Health(1));

        world.Remove<Health>(target);

        Assert.That(fired, Is.EqualTo(1));
        Assert.That(world.IsAlive(target), Is.False);
        Assert.That(world.EntityCount, Is.Zero);
    }

    [Test]
    public void ADespawnHookThatRemovesFromOrDespawnsItsOwnEntityDoesNotFireItselfAgain()
    {
        var world = new World();
        var fired = 0;
        world.OnRemove((Entity entity, in Health _) =>
        {
            fired++;
            world.Remove<Health>(entity);
            world.Despawn(entity);
        });
        var parent = world.Spawn();
        var child = world.Spawn();
        world.Add(parent, new Health(1));
        world.Add(child, new Health(2));
        world.SetParent(child, parent);

        world.Despawn(parent);

        Assert.That(fired, Is.EqualTo(2));
        Assert.That(world.EntityCount, Is.Zero);
    }

    [Test]
    public void ADespawnHookCannotGiveItsEntityANewComponent()
    {
        var world = new World();
        Exception? thrown = null;
        world.OnRemove((Entity entity, in Health _) => thrown = Assert.Catch(() => world.Add(entity, new Armor(1))));
        var target = world.Spawn();
        world.Add(target, new Health(1));

        world.Despawn(target);

        Assert.That(thrown, Is.InstanceOf<InvalidOperationException>());
        Assert.That(world.IsAlive(target), Is.False);
    }

    [Test]
    public void ADespawnWhoseHookThrowsLeavesTheEntitiesAliveAndDespawnableAgain()
    {
        var world = new World();
        var shouldThrow = true;
        world.OnRemove((Entity _, in Health _) =>
        {
            if (shouldThrow) throw new InvalidOperationException("hook failed");
        });
        var parent = world.Spawn();
        var child = world.Spawn();
        world.Add(parent, new Health(1));
        world.SetParent(child, parent);

        Assert.Throws<InvalidOperationException>(() => world.Despawn(parent));
        Assert.That(world.IsAlive(parent) && world.IsAlive(child), Is.True);

        shouldThrow = false;
        world.Despawn(parent);

        Assert.That(world.EntityCount, Is.Zero);
    }

    [Test]
    public void SavingOrRestoringASnapshotFromAHookThrowsAndLeavesTheWorldIntact()
    {
        var world = new World();
        var snapshot = new WorldSnapshot();
        var target = world.Spawn();
        world.Add(target, new Health(1));
        world.SaveSnapshot(snapshot);
        var saveFailed = false;
        world.OnRemove((Entity _, in Health _) =>
        {
            saveFailed = Throws(() => world.SaveSnapshot(new WorldSnapshot()));
            world.RestoreSnapshot(snapshot);
        });

        Assert.Throws<InvalidOperationException>(() => world.Despawn(target));
        Assert.Throws<InvalidOperationException>(() => world.Remove<Health>(target));
        Assert.That(saveFailed, Is.True);
        Assert.That(world.IsAlive(target), Is.True);
        Assert.That(world.Has<Health>(target), Is.True);
        Assert.That(world.EntityCount, Is.EqualTo(1));
    }

    [Test]
    public void RestoringASnapshotFromAnAddHookThrows()
    {
        var world = new World();
        var snapshot = new WorldSnapshot();
        world.SaveSnapshot(snapshot);
        world.OnAdd((Entity _, in Health _) => world.RestoreSnapshot(snapshot));
        var target = world.Spawn();

        Assert.Throws<InvalidOperationException>(() => world.Add(target, new Health(1)));
        Assert.That(world.IsAlive(target), Is.True);
    }

    [Test]
    public void LaterRemoveHooksDoNotRunOnceAnEarlierOneDespawnedTheEntity()
    {
        var world = new World();
        var later = 0;
        world.OnRemove((Entity entity, in Health _) => world.Despawn(entity));
        world.OnRemove((Entity _, in Health _) => later++);
        var target = world.Spawn();
        world.Add(target, new Health(1));

        world.Remove<Health>(target);

        Assert.That(later, Is.Zero);
        Assert.That(world.IsAlive(target), Is.False);
    }

    [Test]
    public void LaterAddHooksDoNotRunOnceAnEarlierOneRemovedTheComponent()
    {
        var world = new World();
        var later = 0;
        world.OnAdd((Entity entity, in Health _) => world.Remove<Health>(entity));
        world.OnAdd((Entity _, in Health _) => later++);
        var target = world.Spawn();

        world.Add(target, new Health(1));

        Assert.That(later, Is.Zero);
        Assert.That(world.Has<Health>(target), Is.False);
    }

    [Test]
    public void EachHookSeesTheComponentWhereAnEarlierHookMovedIt()
    {
        var world = new World();
        var seen = new List<int>();
        world.OnAdd((Entity entity, in Health _) => world.Add(entity, new Speed(1f)));
        world.OnAdd((Entity _, in Health health) => seen.Add(health.Value));
        var target = world.Spawn();

        world.Add(target, new Health(5));

        Assert.That(seen, Is.EqualTo(new[] { 5 }));
    }

    private static bool Throws(Action action)
    {
        try
        {
            action();
            return false;
        }
        catch (InvalidOperationException)
        {
            return true;
        }
    }

    private sealed class Counter
    {
        public int Value;
    }
}
