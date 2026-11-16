using System.Numerics;
using Rin.Core.Shared.Math;
using Rin.World.Physics;
using Rin.World.Physics.Bepu;

namespace Rin.World.Tests.Physics;

public class ContactEventTests : BepuFixture
{
    private const float Step = 1f / 60f;

    private List<PhysicsContactEvent> Run(int steps, Action<int>? each = null)
    {
        var events = new List<PhysicsContactEvent>();
        for (var i = 0; i < steps; i++)
        {
            each?.Invoke(i);
            Bepu.Update(Step);
            events.AddRange(Bepu.ContactEvents.ToArray());
        }

        return events;
    }

    [Test]
    public void DynamicBodyLandingOnStaticReportsOneBeginAndNoEndWhileResting()
    {
        var floor = Bepu.CreateBox(new Vector3(10, 1, 10), At(Vector3.Zero), PhysicsState.Static);
        var ball = Bepu.CreateSphere(0.5f, At(new Vector3(0, 2, 0)), PhysicsState.Simulated);

        var events = Run(600);

        Assert.That(events.Count(e => e.Kind == PhysicsContactKind.Begin), Is.EqualTo(1));
        Assert.That(events.Count(e => e.Kind == PhysicsContactKind.End), Is.Zero);
        Assert.That(events[0].IsTrigger, Is.False);
        Assert.That(events[0].Other(ball), Is.EqualTo(floor));
        Assert.That(Bepu.GetPosition(ball).Y, Is.GreaterThan(0.9f), "the ball should rest on the floor, not fall through");
    }

    [Test]
    public void TriggerLetsADynamicBodyThroughAndReportsBeginThenEnd()
    {
        var trigger = Bepu.CreateBox(new Vector3(10, 1, 10), At(Vector3.Zero), PhysicsState.Static);
        Bepu.SetTrigger(trigger, true);
        var ball = Bepu.CreateSphere(0.5f, At(new Vector3(0, 2, 0)), PhysicsState.Simulated);

        var events = Run(240);

        Assert.That(events.Select(e => (e.Kind, e.IsTrigger)),
            Is.EqualTo(new[] { (PhysicsContactKind.Begin, true), (PhysicsContactKind.End, true) }));
        Assert.That(Bepu.GetPosition(ball).Y, Is.LessThan(-1f), "the ball should fall through the trigger");
    }

    [Test]
    public void StaticTriggerSeesAKinematicBodyMovedThroughIt()
    {
        var trigger = Bepu.CreateBox(new Vector3(2, 2, 2), At(Vector3.Zero), PhysicsState.Static);
        Bepu.SetTrigger(trigger, true);
        var mover = Bepu.CreateSphere(0.25f, At(new Vector3(-5, 0, 0)), PhysicsState.Controlled);

        var events = Run(100, i => Bepu.SetPosition(mover, new Vector3(-5 + i * 0.1f, 0, 0)));

        Assert.That(events.Select(e => e.Kind), Is.EqualTo(new[] { PhysicsContactKind.Begin, PhysicsContactKind.End }));
        Assert.That(events[0].Other(trigger), Is.EqualTo(mover));
    }

    [Test]
    public void DestroyingATouchingBodyEndsThePair()
    {
        Bepu.CreateBox(new Vector3(10, 1, 10), At(Vector3.Zero), PhysicsState.Static);
        var ball = Bepu.CreateSphere(0.5f, At(new Vector3(0, 1.2f, 0)), PhysicsState.Simulated);

        var before = Run(120);
        Bepu.DestroyBody(ball);
        var after = Run(2);

        Assert.That(before.Count(e => e.Kind == PhysicsContactKind.Begin), Is.EqualTo(1));
        Assert.That(after.Select(e => e.Kind), Is.EqualTo(new[] { PhysicsContactKind.End }));
    }
}
