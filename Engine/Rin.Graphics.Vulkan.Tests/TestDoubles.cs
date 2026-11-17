using Rin.Core.Graphics;
using Rin.Core.Shared;

namespace Rin.Graphics.Vulkan.Tests;

/// <summary>
///     Owns the <see cref="UploadLanes" /> of a test and disposes whatever is still queued afterwards.
/// </summary>
public abstract class UploadLanesFixture
{
    private protected UploadLanes Lanes { get; private set; } = null!;

    [SetUp]
    public void CreateLanes()
    {
        Lanes = new UploadLanes();
    }

    [TearDown]
    public void DisposeQueuedActions()
    {
        List<PendingResourceAction> remaining = [];
        Lanes.TakeAll(remaining);
        foreach (var action in remaining) action.Dispose();
    }

    private protected static ResourceHandle Buffer(uint id) => new(ResourceType.Buffer, id);
    private protected static ResourceHandle Texture(uint id) => new(ResourceType.Texture, id);

    private protected PendingBufferWrite QueueWrite(uint id, ulong offset, int bytes = 16)
    {
        var write = new PendingBufferWrite(PooledMemory<byte>.CopyFrom(new byte[bytes]), offset);
        Lanes.EnqueueBuffer(Buffer(id), write);
        return write;
    }

    private protected SharedWrite QueueCopy(uint source, uint destination)
    {
        var shared = new SharedWrite(new BufferCopy(Buffer(source), Buffer(destination), 0, 0, 16));
        Lanes.EnqueueShared(shared);
        return shared;
    }
}
