
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
}
