using System.Numerics;
using Rin.Core.Shared.Math;
using Rin.World.Scheduling;
using Rin.World.Transforms;

namespace Rin.World.Tests.Transforms;

public class TransformPropagationTests
{
    private static readonly Quaternion QuarterTurnY = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2);

    [Test]
    public void RootWorldMatrixIsItsLocalMatrix()
    {
        var world = new World();
        var root = world.Spawn();
        world.SetPosition(root, new Vector3(1, 2, 3));

        world.Update(0f);

        Assert.That(world.Get<WorldTransform>(root).Matrix.Translation, Is.EqualTo(new Vector3(1, 2, 3)));
        Assert.That(world.Get<LocalTransform>(root).IsDirty, Is.False);
    }

    [Test]
    public void ChildMatchesTheActorModelsParentSpaceMath()
    {
        var world = new World();
        var parent = world.Spawn();
        var child = world.Spawn();
        var parentLocal = new LocalTransform { Position = new Vector3(10, 0, 0), Rotation = QuarterTurnY, Scale = new Vector3(2) };
        var childLocal = new LocalTransform { Position = new Vector3(0, 0, 1) };
        world.SetLocalTransform(parent, parentLocal);
        world.SetLocalTransform(child, childLocal);
        world.SetParent(child, parent);

        world.Update(0f);

        var expected = new Transform { Position = childLocal.Position }
            .InParentSpace(new Transform { Position = parentLocal.Position, Orientation = parentLocal.Rotation, Scale = parentLocal.Scale });
        var actual = world.Get<WorldTransform>(child).Matrix.Translation;
        Assert.That(Vector3.Distance(actual, expected.Position), Is.LessThan(1e-5f));
        Assert.That(Vector3.Distance(actual, new Vector3(12, 0, 0)), Is.LessThan(1e-5f));
    }

    [Test]
    public void MovingTheRootUpdatesEveryDescendant()
    {
        var world = new World();
        var (root, child, grandchild) = Chain(world);
        world.Update(0f);

        world.SetPosition(root, new Vector3(5, 0, 0));
        world.Update(0f);

        Assert.That(world.Get<WorldTransform>(child).Matrix.Translation, Is.EqualTo(new Vector3(6, 0, 0)));
        Assert.That(world.Get<WorldTransform>(grandchild).Matrix.Translation, Is.EqualTo(new Vector3(7, 0, 0)));
    }

    [Test]
    public void VersionChangesOnlyWhenTheMatrixChanges()
    {
        var world = new World();
        var entity = world.Spawn();
        world.SetPosition(entity, new Vector3(1, 0, 0));
        world.Update(0f);
        Assert.That(world.Get<WorldTransform>(entity).Version, Is.EqualTo(1));

        world.Update(0f);
        Assert.That(world.Get<WorldTransform>(entity).Version, Is.EqualTo(1), "nothing was dirty");

        world.SetPosition(entity, new Vector3(1, 0, 0));
        world.Update(0f);
        Assert.That(world.Get<WorldTransform>(entity).Version, Is.EqualTo(1), "dirty but the matrix is the same");

        world.SetPosition(entity, new Vector3(2, 0, 0));
        world.Update(0f);
        Assert.That(world.Get<WorldTransform>(entity).Version, Is.EqualTo(2));
    }

    [Test]
    public void MovingAChildLeavesItsParentAndSiblingUntouched()
    {
        var world = new World();
        var parent = world.Spawn();
        var moved = world.Spawn();
        var sibling = world.Spawn();
        world.SetPosition(parent, Vector3.UnitX);
        world.SetPosition(moved, Vector3.UnitY);
        world.SetPosition(sibling, Vector3.UnitZ);
        world.SetParent(moved, parent);
        world.SetParent(sibling, parent);
        world.Update(0f);

        world.SetPosition(moved, Vector3.UnitY * 2);
        world.Update(0f);

        Assert.That(world.Get<WorldTransform>(moved).Version, Is.EqualTo(2));
        Assert.That(world.Get<WorldTransform>(parent).Version, Is.EqualTo(1));
        Assert.That(world.Get<WorldTransform>(sibling).Version, Is.EqualTo(1));
    }

    [Test]
    public void ReparentingRecomputesAgainstTheNewParent()
    {
        var world = new World();
        var first = world.Spawn();
        var second = world.Spawn();
        var child = world.Spawn();
        world.SetPosition(first, new Vector3(10, 0, 0));
        world.SetPosition(second, new Vector3(0, 20, 0));
        world.SetPosition(child, new Vector3(1, 0, 0));
        world.SetParent(child, first);
        world.Update(0f);
        Assert.That(world.Get<WorldTransform>(child).Matrix.Translation, Is.EqualTo(new Vector3(11, 0, 0)));

        world.SetParent(child, second);
        world.Update(0f);
        Assert.That(world.Get<WorldTransform>(child).Matrix.Translation, Is.EqualTo(new Vector3(1, 20, 0)));

        world.ClearParent(child);
        world.Update(0f);
        Assert.That(world.Get<WorldTransform>(child).Matrix.Translation, Is.EqualTo(new Vector3(1, 0, 0)));
    }

    [Test]
    public void WritesThroughAQuerySpanArePropagated()
    {
        var world = new World { TickInterval = 0.125f };
        var (root, child, _) = Chain(world);
        world.AddSystem(TickPhase.Gameplay, new MoveRootsUp(world));
        world.Update(0f);

        world.Update(0.125f);

        Assert.That(world.Get<WorldTransform>(root).Matrix.Translation, Is.EqualTo(new Vector3(0, 1, 0)));
        Assert.That(world.Get<WorldTransform>(child).Matrix.Translation, Is.EqualTo(new Vector3(1, 1, 0)));
    }

    [Test]
    public void SetParentMarksTheChildDirty()
    {
        var world = new World();
        var parent = world.Spawn();
        var child = world.Spawn();
        world.SetPosition(parent, Vector3.UnitX);
        world.SetPosition(child, Vector3.UnitX);
        world.Update(0f);

        world.SetParent(child, parent);

        Assert.That(world.Get<LocalTransform>(child).IsDirty, Is.True);
    }

    [Test]
    public void PropagationDoesNotAllocate()
    {
        var world = new World();
        var roots = new Entity[16];
        for (var i = 0; i < roots.Length; i++)
        {
            var (root, _, _) = Chain(world);
            roots[i] = root;
        }

        MoveAndUpdate(world, roots, 0);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 1; frame <= 50; frame++) MoveAndUpdate(world, roots, frame);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.That(allocated, Is.Zero);
    }

    private sealed class MoveRootsUp(World world) : TickSystem
    {
        private readonly Query<LocalTransform> _transforms = world.Query<LocalTransform>();

        public override void Tick(in TickContext context)
        {
            foreach (var (transforms, entities) in _transforms)
                for (var i = 0; i < transforms.Length; i++)
                    if (!context.World.GetParent(entities[i]).IsValid)
                        transforms[i].Position += Vector3.UnitY;
        }
    }

    private static void MoveAndUpdate(World world, Entity[] roots, int frame)
    {
        foreach (var root in roots) world.SetPosition(root, new Vector3(frame, 0, 0));
        world.Update(1f / 60f);
    }

    private static (Entity Root, Entity Child, Entity Grandchild) Chain(World world)
    {
        var root = world.Spawn();
        var child = world.Spawn();
        var grandchild = world.Spawn();
        world.SetPosition(root, Vector3.Zero);
        world.SetPosition(child, Vector3.UnitX);
        world.SetPosition(grandchild, Vector3.UnitX);
        world.SetParent(child, root);
        world.SetParent(grandchild, child);
        return (root, child, grandchild);
    }
}
