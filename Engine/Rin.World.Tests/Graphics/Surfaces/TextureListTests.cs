using Rin.Core.Graphics;
using Rin.World.Graphics.Surfaces;

namespace Rin.World.Tests.Graphics.Surfaces;

public class TextureListTests
{
    [Test]
    public void UnsetHandlesAreSkipped()
    {
        var list = new TextureList(stackalloc DeviceHandle[4]);
        list.Add(new DeviceHandle(ResourceType.Texture, 0));
        list.Add(new DeviceHandle(ResourceType.Texture, 7));

        Assert.That(list.Count, Is.EqualTo(1));
        Assert.That(list.AsSpan()[0].Id, Is.EqualTo(7u));
    }

    [Test]
    public void AddingPastCapacityThrows()
    {
        Assert.Throws<InvalidOperationException>(() =>
        {
            var list = new TextureList(stackalloc DeviceHandle[1]);
            list.Add(new DeviceHandle(ResourceType.Texture, 1));
            list.Add(new DeviceHandle(ResourceType.Texture, 2));
        });
    }
}
