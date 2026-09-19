using Rin.Core.Graphics;

namespace Rin.Core.Tests.Graphics;

public class ResourceHandleTests
{
    [TestCase(ResourceType.Texture, 1u, false)]
    [TestCase(ResourceType.Cubemap, 42u, true)]
    [TestCase(ResourceType.TextureArray, 0xFFFFFFu, true)] // max 24-bit id
    [TestCase(ResourceType.Buffer, 0u, false)]
    public void ConstructorPacksTypeIdAndBindlessFlagAndAllThreeReadBackUnchanged(ResourceType type, uint id, bool isBindless)
    {
        var handle = new ResourceHandle(type, id, isBindless);

        Assert.That(handle.Type, Is.EqualTo(type));
        Assert.That(handle.Id, Is.EqualTo(id));
        Assert.That(handle.IsBindless, Is.EqualTo(isBindless));
    }

    [Test]
    public void InvalidHandlesHaveIdZero()
    {
        Assert.That(ResourceHandle.InvalidTexture.Id, Is.EqualTo(0u));
        Assert.That(ResourceHandle.InvalidCubemap.Id, Is.EqualTo(0u));
        Assert.That(ResourceHandle.InvalidTextureArray.Id, Is.EqualTo(0u));
        Assert.That(ResourceHandle.InvalidBuffer.Id, Is.EqualTo(0u));
    }

    [Test]
    public void InvalidHandleReportsNotValidWithoutTouchingAGraphicsModule()
    {
        // IsValid() short-circuits on Id == 0, so this must not throw despite no IGraphicsModule being registered.
        Assert.That(ResourceHandle.InvalidTexture.IsValid(), Is.False);
    }

    [Test]
    public void UintConversionRoundTrips()
    {
        var handle = new ResourceHandle(ResourceType.Cubemap, 12345, isBindless: true);

        var raw = (uint)handle;
        var restored = (ResourceHandle)raw;

        Assert.That(restored, Is.EqualTo(handle));
        Assert.That(restored.Type, Is.EqualTo(ResourceType.Cubemap));
        Assert.That(restored.Id, Is.EqualTo(12345u));
        Assert.That(restored.IsBindless, Is.True);
    }

    [Test]
    public void DifferentTypesWithTheSameIdAreNotEqual()
    {
        var texture = new ResourceHandle(ResourceType.Texture, 7);
        var buffer = new ResourceHandle(ResourceType.Buffer, 7);

        Assert.That(texture, Is.Not.EqualTo(buffer));
    }
}
