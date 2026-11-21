
namespace Rin.World.Tests;

public class WorldHierarchyTests
{
    [Test]
    public void SetParentLinksBothWays()
    {
        var world = new World();
        var parent = world.Spawn();
        var first = world.Spawn();
        var second = world.Spawn();

        world.SetParent(first, parent);
        world.SetParent(second, parent);

        Assert.That(world.GetParent(first), Is.EqualTo(parent));
        Assert.That(world.GetParent(parent).IsValid, Is.False);
        Assert.That(Children(world, parent), Is.EqualTo(new[] { first, second }));
    }

    [Test]
    public void SetParentMovesAChildFromItsOldParent()
    {
        var world = new World();
        var oldParent = world.Spawn();
        var newParent = world.Spawn();
        var child = world.Spawn();
        world.SetParent(child, oldParent);

        world.SetParent(child, newParent);

        Assert.That(world.GetParent(child), Is.EqualTo(newParent));
        Assert.That(world.GetChildren(oldParent).Count, Is.Zero);
    }

    [Test]
    public void ClearParentMakesARoot()
    {
        var world = new World();
        var parent = world.Spawn();
        var child = world.Spawn();
        world.SetParent(child, parent);

        world.ClearParent(child);

        Assert.That(world.GetParent(child).IsValid, Is.False);
        Assert.That(world.GetChildren(parent).Count, Is.Zero);
    }

    [Test]
    public void ParentingAnAncestorUnderItsDescendantThrows()
    {
        var world = new World();
        var root = world.Spawn();
        var child = world.Spawn();
        world.SetParent(child, root);

        Assert.Throws<InvalidOperationException>(() => world.SetParent(root, child));
    }

    [Test]
    public void ParentingAnEntityUnderItselfThrows()
    {
        var world = new World();
        var entity = world.Spawn();

        Assert.Throws<InvalidOperationException>(() => world.SetParent(entity, entity));
    }

    [Test]
    public void ClearingAChildKeepsTheOrderOfTheOthers()
    {
        var world = new World();
        var parent = world.Spawn();
        var children = new[] { world.Spawn(), world.Spawn(), world.Spawn() };
        foreach (var child in children) world.SetParent(child, parent);

        world.ClearParent(children[1]);

        Assert.That(Children(world, parent), Is.EqualTo(new[] { children[0], children[2] }));
    }

    [Test]
    public void AReusedIdStartsWithoutParentOrChildren()
    {
        var world = new World();
        var parent = world.Spawn();
        var child = world.Spawn();
        world.SetParent(child, parent);
        world.Despawn(parent);

        var reused = world.Spawn();

        Assert.That(world.GetChildren(reused).Count, Is.Zero);
        Assert.That(world.GetParent(reused).IsValid, Is.False);
    }

    [Test]
    public void DespawningAParentDespawnsAllDescendants()
    {
        var world = new World();
        var root = world.Spawn();
        var child = world.Spawn();
        var grandchild = world.Spawn();
        var bystander = world.Spawn();
        world.SetParent(child, root);
        world.SetParent(grandchild, child);

        world.Despawn(root);

        Assert.That(world.IsAlive(root), Is.False);
        Assert.That(world.IsAlive(child), Is.False);
        Assert.That(world.IsAlive(grandchild), Is.False);
        Assert.That(world.IsAlive(bystander), Is.True);
        Assert.That(world.EntityCount, Is.EqualTo(1));
    }

    [Test]
    public void ParentingUnderAnEntityBeingDespawnedThrows()
    {
        var world = new World();
        var doomed = world.Spawn();
        var outsider = world.Spawn();
        world.Add(doomed, new Health(1));
        world.OnRemove((Entity entity, ref Health _) => world.SetParent(outsider, entity));

        Assert.Throws<InvalidOperationException>(() => world.Despawn(doomed));
        Assert.That(world.GetParent(outsider).IsValid, Is.False);
    }

    [Test]
    public void MovingADescendantOfAnEntityBeingDespawnedThrows()
    {
        var world = new World();
        var doomed = world.Spawn();
        var child = world.Spawn();
        var refuge = world.Spawn();
        world.Add(doomed, new Health(1));
        world.SetParent(child, doomed);
        var moveFailed = false;
        var clearFailed = false;
        world.OnRemove((Entity _, ref Health _) =>
        {
            moveFailed = Throws(() => world.SetParent(child, refuge));
            clearFailed = Throws(() => world.ClearParent(child));
        });

        world.Despawn(doomed);

        Assert.That(moveFailed && clearFailed, Is.True);
        Assert.That(world.IsAlive(child), Is.False);
        Assert.That(world.GetChildren(refuge).Count, Is.Zero);
    }

    [Test]
    public void DespawningAChildLeavesTheParent()
    {
        var world = new World();
        var parent = world.Spawn();
        var child = world.Spawn();
        world.SetParent(child, parent);

        world.Despawn(child);

        Assert.That(world.IsAlive(parent), Is.True);
        Assert.That(world.GetChildren(parent).Count, Is.Zero);
    }

    [Test]
    public void EnumeratingChildrenDoesNotAllocate()
    {
        var world = new World();
        var parent = world.Spawn();
        for (var i = 0; i < 8; i++) world.SetParent(world.Spawn(), parent);
        var sum = SumChildIds(world, parent);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 100; i++) sum += SumChildIds(world, parent);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.That(allocated, Is.Zero);
        Assert.That(sum, Is.Positive);
    }

    private static int SumChildIds(World world, Entity parent)
    {
        var sum = 0;
        foreach (var child in world.GetChildren(parent)) sum += child.Id;
        return sum;
    }

    private static List<Entity> Children(World world, Entity parent)
    {
        var children = new List<Entity>();
        foreach (var child in world.GetChildren(parent)) children.Add(child);
        return children;
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
}
