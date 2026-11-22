using System.Numerics;
using Rin.Core.Shared.Math;
using Rin.World.Physics;

namespace Rin.World.Tests.Physics;

public class CompoundBodyTests : BepuFixture
{
    private static readonly ShapePart[] TwoBoxes =
    [
        new(ColliderShape.Box(new Vector3(1f)), new Vector3(-2f, 0f, 0f)),
        new(ColliderShape.Box(new Vector3(1f)), new Vector3(2f, 0f, 0f))
    ];

    [Test]
    public void BothBoxesOfACompoundStopAFallingSphere()
    {
        var table = Bepu.CreateCompound(TwoBoxes, At(Vector3.Zero), PhysicsState.Static);
        var left = Bepu.CreateSphere(0.25f, At(new Vector3(-2f, 3f, 0f)), PhysicsState.Simulated);
        var right = Bepu.CreateSphere(0.25f, At(new Vector3(2f, 3f, 0f)), PhysicsState.Simulated);

        for (var i = 0; i < 240; i++) Bepu.Update(1f / 60f);

        Assert.That(Bepu.GetPosition(left).Y, Is.EqualTo(0.75f).Within(0.05f));
        Assert.That(Bepu.GetPosition(right).Y, Is.EqualTo(0.75f).Within(0.05f));
        Assert.That(Bepu.RayCast(new Vector3(0f, 3f, 0f), -Vector3.UnitY, 10f)?.Body, Is.Not.EqualTo(table),
            "the gap between the boxes is empty");
    }

    [Test]
    public void AnOffCentreCompoundReportsAndTurnsAboutTheEntityOrigin()
    {
        var parts = new[] { new ShapePart(ColliderShape.Box(new Vector3(1f)), new Vector3(3f, 0f, 0f)) };
        var body = Bepu.CreateCompound(parts, At(Vector3.Zero), PhysicsState.Controlled);

        Assert.That(Bepu.GetPosition(body), Is.EqualTo(Vector3.Zero));
        Bepu.SetOrientation(body, Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI));

        Assert.That(Vector3.Distance(Bepu.GetPosition(body), Vector3.Zero), Is.LessThan(1e-4f));
        Bepu.Update(1f / 60f);
        Assert.That(Bepu.RayCast(new Vector3(-3f, 5f, 0f), -Vector3.UnitY, 10f)?.Body, Is.EqualTo(body),
            "the part swung round to -X");
    }

    [Test]
    public void ASimulatedCompoundLandsWithItsEntityOriginWhereItsPartsPutIt()
    {
        Bepu.CreateBox(new Vector3(20f, 1f, 20f), At(new Vector3(0f, -0.5f, 0f)), PhysicsState.Static);
        var parts = new[] { new ShapePart(ColliderShape.Box(new Vector3(1f)), new Vector3(0f, 1f, 0f)) };
        var body = Bepu.CreateCompound(parts, At(new Vector3(0f, 2f, 0f)), PhysicsState.Simulated);

        for (var i = 0; i < 240; i++) Bepu.Update(1f / 60f);

        Assert.That(Bepu.GetPosition(body).Y, Is.EqualTo(-0.5f).Within(0.05f), "the part rests on the floor, 1 m above the origin");
    }

    [Test]
    public void ACompoundsScaleIsFixedAtCreation()
    {
        var body = Bepu.CreateCompound(TwoBoxes, At(Vector3.Zero), PhysicsState.Static);

        Assert.Throws<NotSupportedException>(() => Bepu.SetScale(body, new Vector3(2f)));
    }

    [Test]
    public void DestroyingACompoundReleasesItsParts()
    {
        var body = Bepu.CreateCompound(TwoBoxes, At(Vector3.Zero), PhysicsState.Simulated);

        Bepu.DestroyBody(body);

        Assert.That(Bepu.RayCast(new Vector3(-2f, 5f, 0f), -Vector3.UnitY, 10f), Is.Null);
    }
}
