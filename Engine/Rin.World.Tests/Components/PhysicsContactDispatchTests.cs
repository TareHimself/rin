using System.Numerics;
using Rin.World.Actors;
using Rin.World.Components;
using Rin.World.Physics;

namespace Rin.World.Tests.Components;

public class PhysicsContactDispatchTests
{
    [Test]
    public void ContactEventsReachBothComponentsWithEachOtherAsTheOtherParty()
    {
        var render = new FakeRenderSystem();
        var physics = new FakePhysicsSystem();
        var world = new World(render, physics);
        var a = new BoxPhysicsComponent { Size = Vector3.One };
        var b = new SpherePhysicsComponent { Radius = 1f, IsTrigger = true };
        world.Start();
        world.AddActor(new Actor { RootComponent = a });
        var handleA = physics.LastCreated;
        world.AddActor(new Actor { RootComponent = b });
        var handleB = physics.LastCreated;

        var seen = new List<string>();
        a.OnOverlapBegin += (self, other) => seen.Add($"a:begin:{ReferenceEquals(other, b)}");
        a.OnOverlapEnd += (self, other) => seen.Add($"a:end:{ReferenceEquals(other, b)}");
        b.OnOverlapBegin += (self, other) => seen.Add($"b:begin:{ReferenceEquals(other, a)}");
        a.OnCollisionBegin += (self, other) => seen.Add("a:collision");

        physics.EmitContact(handleA, handleB, PhysicsContactKind.Begin, trigger: true);
        world.Update(world.PhysicsUpdateInterval);
        physics.EmitContact(handleA, handleB, PhysicsContactKind.End, trigger: true);
        world.Update(world.PhysicsUpdateInterval);

        Assert.That(seen, Is.EqualTo(new[] { "a:begin:True", "b:begin:True", "a:end:True" }));
        Assert.That(physics.GetTrigger(handleB), Is.True);
    }
}
