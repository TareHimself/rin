using System.Numerics;
using Examples.Editor.Model;
using Rin.World;
using Rin.World.Physics;
using Rin.World.Transforms;

namespace Examples.Tests.Editor.Model;

public class ComponentRequirementsTests
{
    [Test]
    public void AddingAVelocityBringsALocalTransformAndARigidBodyFirst()
    {
        var world = new World();
        var entity = world.Spawn();
        var velocity = world.Schema.IdOf<Velocity>();

        ComponentRequirements.AddWithRequirements(world, entity, velocity, []);

        Assert.That(world.Has<LocalTransform>(entity), Is.True);
        Assert.That(world.Has<RigidBody>(entity), Is.True);
        Assert.That(world.Has<Velocity>(entity), Is.True);
    }

    [Test]
    public void OnlyTheRequirementsTheEntityLacksAreMissing()
    {
        var world = new World();
        var entity = world.Spawn();
        world.SetPosition(entity, Vector3.Zero);
        var missing = new List<ComponentId>();

        ComponentRequirements.CollectMissing(world, entity, world.Schema.IdOf<Velocity>(), missing);

        Assert.That(missing, Is.EqualTo(new[] { world.Schema.IdOf<RigidBody>() }));
    }

    [Test]
    public void ALocalTransformIsNeededByTheRigidBodyThatRequiresIt()
    {
        var world = new World();
        var entity = world.Spawn();
        world.SetPosition(entity, Vector3.Zero);
        world.Add(entity, new RigidBody());
        var dependents = new List<ComponentId>();

        ComponentRequirements.CollectDependents(world, entity, world.Schema.IdOf<LocalTransform>(), dependents);

        Assert.That(dependents, Is.EqualTo(new[] { world.Schema.IdOf<RigidBody>() }));
    }
}
