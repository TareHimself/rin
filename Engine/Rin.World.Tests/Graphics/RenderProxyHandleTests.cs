using Rin.World.Graphics;

namespace Rin.World.Tests.Graphics;

public class RenderProxyHandleTests
{
    [Test]
    public void DefaultRenderProxyHandleIsInvalid()
    {
        Assert.That(RenderProxyHandle.Invalid.IsValid, Is.False);
        Assert.That(default(RenderProxyHandle).IsValid, Is.False);
    }

    [Test]
    public void RenderProxyHandleWithNonZeroVersionIsValid()
    {
        Assert.That(new RenderProxyHandle(0, 1).IsValid, Is.True);
    }

    [Test]
    public void RenderProxyHandlesWithDifferentVersionsAreNotEqual()
    {
        var first = new RenderProxyHandle(0, 1);
        var second = new RenderProxyHandle(0, 2);

        Assert.That(first, Is.Not.EqualTo(second),
            "a stale handle from before a slot was reused must not be mistaken for the new occupant");
    }
}
