using Rin.World.Graphics;

namespace Rin.World.Tests.Graphics;

public class CameraTests
{
    [Test]
    public void AnEv100OfZeroExposesByTheSensorConstant()
    {
        Assert.That(new Camera { Ev100 = 0f }.Exposure, Is.EqualTo(1f / 1.2f).Within(1e-6f));
    }

    [Test]
    public void EachStopOfEv100HalvesTheExposure()
    {
        var exposure = new Camera { Ev100 = 9f }.Exposure;
        var oneStopUp = new Camera { Ev100 = 10f }.Exposure;

        Assert.That(oneStopUp, Is.EqualTo(exposure / 2f).Within(1e-9f));
    }
}
