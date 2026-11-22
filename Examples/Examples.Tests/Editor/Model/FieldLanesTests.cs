using System.Numerics;
using Examples.Editor.Model;
using Rin.World;

namespace Examples.Tests.Editor.Model;

public class FieldLanesTests
{
    [Test]
    public void IntegersAreRoundedAndClampedInsteadOfWrappingAround()
    {
        var bytes = new byte[8];

        FieldLanes.Write(FieldKind.UInt8, bytes, 300.6);
        Assert.That(FieldLanes.Read(FieldKind.UInt8, bytes), Is.EqualTo(255));

        FieldLanes.Write(FieldKind.Int16, bytes, -2.6);
        Assert.That(FieldLanes.Read(FieldKind.Int16, bytes), Is.EqualTo(-3));

        FieldLanes.Write(FieldKind.UInt32, bytes, -5);
        Assert.That(FieldLanes.Read(FieldKind.UInt32, bytes), Is.EqualTo(0));
    }

    [Test]
    public void FloatsKeepTheirFraction()
    {
        var bytes = new byte[8];

        FieldLanes.Write(FieldKind.Float32, bytes, 1.25);

        Assert.That(FieldLanes.Read(FieldKind.Float32, bytes), Is.EqualTo(1.25));
    }

    [Test]
    public void EulerAnglesRoundTripThroughAQuaternion()
    {
        var degrees = new Vector3(30f, -60f, 15f);

        var back = FieldLanes.ToEuler(FieldLanes.FromEuler(degrees));

        Assert.That(Vector3.Distance(back, degrees), Is.LessThan(1e-3f));
    }
}
