using System.Numerics;
using Examples.Editor.Model;
using Rin.World;
using Rin.World.Graphics;
using Rin.World.Physics;
using Rin.World.Transforms;

namespace Examples.Tests.Editor.Model;

public class HierarchyRowsTests
{
    [Test]
    public void ChildrenFollowTheirParentOneLevelDeeper()
    {
        var world = new World();
        var parent = world.Spawn();
        var other = world.Spawn();
        var child = world.Spawn();
        world.SetPosition(parent, Vector3.Zero);
        world.SetPosition(child, Vector3.Zero);
        world.SetParent(child, parent);
        var rows = new List<HierarchyRow>();

        HierarchyRows.Build(world, new EntityNames(), [], new HashSet<Entity>(), "", rows);

        Assert.That(rows.Select(r => (r.Entity, r.Depth)), Is.EqualTo(new[] { (parent, 0), (child, 1), (other, 0) }));
    }

    [Test]
    public void ACollapsedParentHidesItsChildrenButStillSaysItHasThem()
    {
        var world = new World();
        var parent = world.Spawn();
        var child = world.Spawn();
        world.SetPosition(parent, Vector3.Zero);
        world.SetPosition(child, Vector3.Zero);
        world.SetParent(child, parent);
        var rows = new List<HierarchyRow>();

        HierarchyRows.Build(world, new EntityNames(), [], new HashSet<Entity> { parent }, "", rows);

        Assert.That(rows.Select(r => r.Entity), Is.EqualTo(new[] { parent }));
        Assert.That(rows[0].HasChildren, Is.True);
        Assert.That(rows[0].IsExpanded, Is.False);
    }

    [Test]
    public void AFilterKeepsMatchesAndTheirAncestorsEvenUnderACollapsedParent()
    {
        var world = new World();
        var post = world.Spawn();
        var lamp = world.Spawn();
        var other = world.Spawn();
        world.SetPosition(post, Vector3.Zero);
        world.SetPosition(lamp, Vector3.Zero);
        world.SetParent(lamp, post);
        world.Add(lamp, new PointLight());
        world.Add(other, new DirectionalLight());
        var rows = new List<HierarchyRow>();

        HierarchyRows.Build(world, new EntityNames(), [], new HashSet<Entity> { post }, "pointlight", rows);

        Assert.That(rows.Select(r => r.Entity), Is.EqualTo(new[] { post, lamp }));
    }

    [Test]
    public void ALabelNamesTheEntitysComponentsButNotItsTransforms()
    {
        var world = new World();
        var light = world.Spawn();
        world.SetPosition(light, Vector3.Zero);
        world.Add(light, new PointLight());
        var crowded = world.Spawn();
        world.Add(crowded, new PointLight());
        world.Add(crowded, new DirectionalLight());
        world.Add(crowded, new RigidBody());

        Assert.That(HierarchyRows.LabelOf(world, light), Is.EqualTo($"#{light.Id} PointLight"));
        Assert.That(HierarchyRows.LabelOf(world, world.Spawn()), Does.EndWith(" Entity"));
        Assert.That(HierarchyRows.LabelOf(world, crowded), Does.EndWith(" +1"));
    }
}
