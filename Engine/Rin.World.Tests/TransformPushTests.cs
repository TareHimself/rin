using System.Numerics;
using Rin.World.Actors;
using Rin.World.Components;
using Rin.World.Physics;

namespace Rin.World.Tests;

public class TransformPushTests
{
    [Test]
    public void PhysicsDrivenParentPushesMeshChildWithNoOneFrameLag()
    {
        var render = new FakeRenderSystem();
        var physics = new FakePhysicsSystem();
        var world = new World(render, physics);

        var root = new BoxPhysicsComponent { PhysicsState = PhysicsState.Simulated };
        var mesh = new TestMeshComponent();
        var actor = new Actor { RootComponent = root, InitialComponents = [mesh] };

        world.Start();
        world.AddActor(actor);

        var newPosition = new Vector3(5, 10, -3);
        physics.OnUpdate = _ => physics.SetPosition(physics.LastCreated, newPosition);

        world.Update(world.PhysicsUpdateInterval);

        Assert.That(render.PushedTransforms, Has.Count.EqualTo(1),
            "the mesh child should push exactly once, in the same frame the physics step moved its parent");
        var pushed = render.PushedTransforms[0];
        Assert.That(pushed.Handle, Is.EqualTo(mesh.Proxy));
        Assert.That(pushed.Transform.Translation.X, Is.EqualTo(newPosition.X).Within(1e-4f));
        Assert.That(pushed.Transform.Translation.Y, Is.EqualTo(newPosition.Y).Within(1e-4f));
        Assert.That(pushed.Transform.Translation.Z, Is.EqualTo(newPosition.Z).Within(1e-4f));
    }

    [Test]
    public void UnmovedComponentProducesNoProxyUpdates()
    {
        var render = new FakeRenderSystem();
        var physics = new FakePhysicsSystem();
        var world = new World(render, physics);

        var mesh = new TestMeshComponent();
        var actor = new Actor { RootComponent = mesh };

        world.Start();
        world.AddActor(actor);

        Assert.That(render.PushedTransforms, Is.Empty,
            "Start() already captures the initial transform in the proxy desc; nothing moved yet");

        world.Update(1f / 60f);
        world.Update(1f / 60f);
        world.Update(1f / 60f);

        Assert.That(render.PushedTransforms, Is.Empty,
            "no component moved across three frames, so LateUpdate should push nothing");

        mesh.SetLocation(new Vector3(1, 2, 3));
        world.Update(1f / 60f);

        Assert.That(render.PushedTransforms, Has.Count.EqualTo(1),
            "exactly one push once the component actually moved");
    }

    [Test]
    public void StoppedComponentDestroysItsProxy()
    {
        var render = new FakeRenderSystem();
        var physics = new FakePhysicsSystem();
        var world = new World(render, physics);

        var mesh = new TestMeshComponent();
        var actor = new Actor { RootComponent = mesh };

        world.Start();
        world.AddActor(actor);
        actor.Stop();

        Assert.That(render.DestroyCount, Is.EqualTo(1));
    }
}
