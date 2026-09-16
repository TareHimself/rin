using System.Numerics;
using Rin.Core.Shared.Math;
using Rin.World.Actors;
using Rin.World.Physics;

namespace Rin.World.Tests;

public class WorldPhysicsFacadeTests
{
    [Test]
    public void RayCastResolvesOwnerThroughWorldFacade()
    {
        var render = new FakeRenderSystem();
        var physics = new FakePhysicsSystem();
        var world = new World(render, physics);

        var mesh = new TestMeshComponent();
        var actor = new Actor { RootComponent = mesh };

        world.Start();
        world.AddActor(actor);

        var handle = world.PhysicsSystem.CreateSphere(1f, new Transform { Position = new Vector3(0, 0, 5), Orientation = Quaternion.Identity, Scale = Vector3.One },
            PhysicsState.Static);
        world.RegisterPhysicsBody(handle, mesh);

        var result = world.PhysicsSystem.RayCast(Vector3.Zero, Vector3.UnitZ, 20f);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.Body, Is.EqualTo(handle));
        Assert.That(world.FindPhysicsOwner(result.Value.Body), Is.SameAs(mesh));
    }

    [Test]
    public void UnregisterPhysicsBodyClearsOwnerRegistration()
    {
        var render = new FakeRenderSystem();
        var physics = new FakePhysicsSystem();
        var world = new World(render, physics);

        var mesh = new TestMeshComponent();
        var handle = world.PhysicsSystem.CreateSphere(1f, new Transform { Position = Vector3.Zero, Orientation = Quaternion.Identity, Scale = Vector3.One },
            PhysicsState.Static);
        world.RegisterPhysicsBody(handle, mesh);

        Assert.That(world.FindPhysicsOwner(handle), Is.SameAs(mesh));

        world.UnregisterPhysicsBody(handle);

        Assert.That(world.FindPhysicsOwner(handle), Is.Null);
    }
}
