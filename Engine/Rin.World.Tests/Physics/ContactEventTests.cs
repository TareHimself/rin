using System.Numerics;
using Rin.Core.Shared.Math;
using Rin.World.Physics;
using Rin.World.Physics.Bepu;

namespace Rin.World.Tests.Physics;

public class ContactEventTests
{
    private const float Step = 1f / 60f;

    private static Transform At(Vector3 position) => new() { Position = position, Orientation = Quaternion.Identity, Scale = Vector3.One };

    private static List<PhysicsContactEvent> Run(BepuPhysicsSystem physics, int steps, Action<int>? each = null)
    {
        var events = new List<PhysicsContactEvent>();
        for (var i = 0; i < steps; i++)
        {
            each?.Invoke(i);
            physics.Update(Step);
            events.AddRange(physics.ContactEvents.ToArray());
        }

        return events;
    }

    [Test]
    public void DynamicBodyLandingOnStaticReportsOneBeginAndNoEndWhileResting()
    {
        var physics = new BepuPhysicsSystem();
        var floor = physics.CreateBox(new Vector3(10, 1, 10), At(Vector3.Zero), PhysicsState.Static);
        var ball = physics.CreateSphere(0.5f, At(new Vector3(0, 2, 0)), PhysicsState.Simulated);

        var events = Run(physics, 600);

        Assert.That(events.Count(e => e.Kind == PhysicsContactKind.Begin), Is.EqualTo(1));
        Assert.That(events.Count(e => e.Kind == PhysicsContactKind.End), Is.Zero);
        Assert.That(events[0].IsTrigger, Is.False);
        Assert.That(events[0].Other(ball), Is.EqualTo(floor));
        Assert.That(physics.GetPosition(ball).Y, Is.GreaterThan(0.9f), "the ball should rest on the floor, not fall through");
    }

    [Test]
    public void TriggerLetsADynamicBodyThroughAndReportsBeginThenEnd()
    {
        var physics = new BepuPhysicsSystem();
        var trigger = physics.CreateBox(new Vector3(10, 1, 10), At(Vector3.Zero), PhysicsState.Static);
        physics.SetTrigger(trigger, true);
        var ball = physics.CreateSphere(0.5f, At(new Vector3(0, 2, 0)), PhysicsState.Simulated);

        var events = Run(physics, 240);

        Assert.That(events.Select(e => (e.Kind, e.IsTrigger)),
            Is.EqualTo(new[] { (PhysicsContactKind.Begin, true), (PhysicsContactKind.End, true) }));
        Assert.That(physics.GetPosition(ball).Y, Is.LessThan(-1f), "the ball should fall through the trigger");
    }

    [Test]
    public void StaticTriggerSeesAKinematicBodyMovedThroughIt()
    {
        var physics = new BepuPhysicsSystem();
        var trigger = physics.CreateBox(new Vector3(2, 2, 2), At(Vector3.Zero), PhysicsState.Static);
        physics.SetTrigger(trigger, true);
        var mover = physics.CreateSphere(0.25f, At(new Vector3(-5, 0, 0)), PhysicsState.Controlled);

        var events = Run(physics, 100, i => physics.SetPosition(mover, new Vector3(-5 + i * 0.1f, 0, 0)));

        Assert.That(events.Select(e => e.Kind), Is.EqualTo(new[] { PhysicsContactKind.Begin, PhysicsContactKind.End }));
        Assert.That(events[0].Other(trigger), Is.EqualTo(mover));
    }

    [Test]
    public void DestroyingATouchingBodyEndsThePair()
    {
        var physics = new BepuPhysicsSystem();
        physics.CreateBox(new Vector3(10, 1, 10), At(Vector3.Zero), PhysicsState.Static);
        var ball = physics.CreateSphere(0.5f, At(new Vector3(0, 1.2f, 0)), PhysicsState.Simulated);

        var before = Run(physics, 120);
        physics.DestroyBody(ball);
        var after = Run(physics, 2);

        Assert.That(before.Count(e => e.Kind == PhysicsContactKind.Begin), Is.EqualTo(1));
        Assert.That(after.Select(e => e.Kind), Is.EqualTo(new[] { PhysicsContactKind.End }));
    }
}
