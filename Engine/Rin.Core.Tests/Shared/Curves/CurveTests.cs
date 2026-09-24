using System.Numerics;
using Rin.Core.Shared.Curves;

namespace Rin.Core.Tests.Shared.Curves;

public class CurveTests
{
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(8)]
    [TestCase(29)]
    [TestCase(30)]
    public void LinearSamplesInterpolateBetweenTheBracketingKeys(int keyCount)
    {
        var curve = new Vector3Curve();
        for (var i = 0; i < keyCount; i++) curve.AddLinear(i, new Vector3(i));

        for (var time = 0f; time <= keyCount - 1; time += 0.05f)
            Assert.That(curve.Sample(time).X, Is.EqualTo(time).Within(1e-3f), $"t={time}");
    }

    [Test]
    public void QuaternionSamplesStayUnitLength()
    {
        var curve = new QuaternionCurve();
        curve.AddLinear(0f, Quaternion.Identity);
        curve.AddLinear(1f, Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI * 0.9f));

        Assert.That(curve.Sample(0.5f).Length(), Is.EqualTo(1f).Within(1e-4f));
    }

    [Test]
    public void QuaternionSamplesTakeTheShortestPathAcrossASignFlip()
    {
        var start = Quaternion.CreateFromAxisAngle(Vector3.UnitY, 0.1f);
        var end = Quaternion.CreateFromAxisAngle(Vector3.UnitY, 0.3f);
        var curve = new QuaternionCurve();
        curve.AddLinear(0f, start);
        curve.AddLinear(1f, Quaternion.Negate(end));

        var sampled = curve.Sample(0.5f);
        var expected = Quaternion.CreateFromAxisAngle(Vector3.UnitY, 0.2f);

        Assert.That(float.Abs(Quaternion.Dot(sampled, expected)), Is.EqualTo(1f).Within(1e-4f),
            "-q and q are the same rotation, so the midpoint must be the 0.2 rad rotation");
    }
}
