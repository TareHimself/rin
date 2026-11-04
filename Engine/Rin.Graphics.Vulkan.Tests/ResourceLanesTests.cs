using Rin.Core.Graphics;
using Rin.Core.Shared;

namespace Rin.Graphics.Vulkan.Tests;

public class ResourceLanesTests
{
    private UploadLanes _lanes = null!;

    [SetUp]
    public void CreateLanes()
    {
        _lanes = new UploadLanes();
    }

    [TearDown]
    public void DisposeActions()
    {
        List<PendingResourceAction> remaining = [];
        _lanes.TakeAll(remaining);
        foreach (var action in remaining) action.Dispose();
    }

    private static ResourceHandle Buffer(uint id) => new(ResourceType.Buffer, id);
    private static ResourceHandle Texture(uint id) => new(ResourceType.Texture, id);

    private PendingTextureWrite TextureWrite(uint id)
    {
        var write = new PendingTextureWrite(PooledMemory<byte>.CopyFrom(new byte[16]), default, new Extent2D(2, 2),
            new Extent2D(4, 4));
        _lanes.EnqueueTexture(Texture(id), write);
        return write;
    }

    private SharedWrite BufferToTexture(uint buffer, uint texture)
    {
        var shared = new SharedWrite(new BufferToTextureCopy(Buffer(buffer), Texture(texture), 0, 16, default,
            new Extent2D(2, 2)));
        _lanes.EnqueueShared(shared);
        return shared;
    }

    private SharedWrite TextureToBuffer(uint texture, uint buffer)
    {
        var shared = new SharedWrite(new TextureToBufferCopy(Texture(texture), Buffer(buffer), default,
            new Extent2D(2, 2), 0, 16));
        _lanes.EnqueueShared(shared);
        return shared;
    }

    private PendingBufferWrite Write(uint id, ulong offset, int bytes = 16)
    {
        var write = new PendingBufferWrite(PooledMemory<byte>.CopyFrom(new byte[bytes]), offset);
        _lanes.EnqueueBuffer(Buffer(id), write);
        return write;
    }

    private SharedWrite Copy(uint source, uint destination)
    {
        var shared = new SharedWrite(new BufferCopy(Buffer(source), Buffer(destination), 0, 0, 16));
        _lanes.EnqueueShared(shared);
        return shared;
    }

    private List<(ResourceHandle Handle, PendingResourceAction Action)> TakeOrdered(int max = 64)
    {
        List<(ResourceHandle, PendingResourceAction)> taken = [];
        _lanes.TakeOrdered(max, taken);
        return taken;
    }

    [Test]
    public void Enqueue_MergesACoveringWriteIntoThePreviousOne_AndAdoptsItsTasks()
    {
        var covered = Write(1, 16);
        var task = new TaskCompletionSource();
        covered.Tasks.Add(task);

        var covering = Write(1, 0, 64);

        Assert.Multiple(() =>
        {
            Assert.That(_lanes.Count(Buffer(1)), Is.EqualTo(1));
            Assert.That(covering.Tasks, Is.EqualTo(new[] { task }));
        });
    }

    [Test]
    public void Enqueue_DoesNotMergeAcrossAFence()
    {
        Write(1, 16);
        Copy(1, 2);

        Write(1, 0, 64);

        Assert.That(_lanes.Count(Buffer(1)), Is.EqualTo(3));
    }

    [Test]
    public void Enqueue_DoesNotMergeIntoItemsThatWerePeeked()
    {
        var peeked = Write(1, 16);
        List<PendingResourceAction> items = [];
        _lanes.Peek(Buffer(1), items);

        Write(1, 0, 64);
        List<PendingResourceAction> taken = [];
        _lanes.Take(Buffer(1), items.Count, taken);

        Assert.Multiple(() =>
        {
            Assert.That(taken, Is.EqualTo(new[] { peeked }));
            Assert.That(_lanes.Count(Buffer(1)), Is.EqualTo(1));
        });
    }

    [Test]
    public void EnqueueCopy_PutsTheSameFenceInBothLanes_AndOnceInTheCopyList()
    {
        var copy = Copy(1, 2);

        List<PendingResourceAction> source = [];
        List<PendingResourceAction> destination = [];
        List<SharedWrite> copies = [];
        _lanes.Peek(Buffer(1), source);
        _lanes.Peek(Buffer(2), destination);
        _lanes.PeekShared(copies);

        Assert.Multiple(() =>
        {
            Assert.That(source, Is.EqualTo(new[] { copy }));
            Assert.That(destination, Is.EqualTo(new[] { copy }));
            Assert.That(copies, Is.EqualTo(new[] { copy }));
        });
    }

    [Test]
    public void TakeOrdered_RunsCopiesInEnqueueOrder_EachWithTheWritesAheadOfItInBothLanes()
    {
        var intoB = Write(2, 0);
        var copy1 = Copy(1, 2);
        var afterCopyOnA = Write(1, 0);
        var afterCopyOnB = Write(2, 16);
        var copy2 = Copy(2, 3);

        var taken = TakeOrdered().Select(t => t.Action).ToList();

        Assert.That(taken, Is.EqualTo(new PendingResourceAction[] { intoB, copy1, afterCopyOnB, copy2, afterCopyOnA }));
    }

    [Test]
    public void TakeOrdered_StopsAtACopyThatDoesNotFit_AndLeavesItsLanesBlockedAtTheFence()
    {
        var firstWrite = Write(1, 0);
        var firstCopy = Copy(1, 2);
        var secondWrite = Write(3, 0);
        var secondCopy = Copy(3, 4);
        var behindFence = Write(3, 16);

        var first = TakeOrdered(3).Select(t => t.Action).ToList();
        var second = TakeOrdered().Select(t => t.Action).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(new PendingResourceAction[] { firstWrite, firstCopy, secondWrite }));
            Assert.That(second, Is.EqualTo(new PendingResourceAction[] { secondCopy, behindFence }));
        });
    }

    [Test]
    public void TakeOrdered_AlwaysTakesTheFirstCopy_EvenWhenItIsBiggerThanTheBudget()
    {
        Write(1, 0);
        Write(1, 16);
        var copy = Copy(1, 2);

        var taken = TakeOrdered(1);

        Assert.That(taken, Has.Count.EqualTo(3));
        Assert.That(taken[^1].Action, Is.SameAs(copy));
    }

    [Test]
    public void Take_OfACopyFromOneLane_RemovesItFromTheCopyList()
    {
        var copy = Copy(1, 2);
        List<PendingResourceAction> taken = [];
        List<SharedWrite> copies = [];

        _lanes.Take(Buffer(1), 1, taken);
        _lanes.PeekShared(copies);

        Assert.Multiple(() =>
        {
            Assert.That(taken, Is.EqualTo(new[] { copy }));
            Assert.That(copy.Taken, Is.True);
            Assert.That(copies, Is.Empty);
            Assert.That(_lanes.Count(Buffer(2)), Is.EqualTo(1));
        });
    }

    [Test]
    public void Drop_CancelsCopiesThatReferenceTheResource_AndRemovesTheirFenceFromTheOtherLane()
    {
        var copy = Copy(1, 2);
        var task = new TaskCompletionSource();
        copy.Tasks.Add(task);
        var write = Write(2, 0);
        List<SharedWrite> copies = [];

        _lanes.Drop(Buffer(1));
        _lanes.PeekShared(copies);

        Assert.Multiple(() =>
        {
            Assert.That(task.Task.IsCanceled, Is.True);
            Assert.That(copies, Is.Empty);
            Assert.That(_lanes.Count(Buffer(1)), Is.Zero);
            Assert.That(_lanes.Count(Buffer(2)), Is.EqualTo(1));
            Assert.That(TakeOrdered().Select(t => t.Action), Is.EqualTo(new PendingResourceAction[] { write }));
        });
    }

    [Test]
    public void EnqueueShared_OnASingleLane_PutsExactlyOneFence_AndTakesWithoutWaitingOnItself()
    {
        var before = Write(1, 0);
        var shared = Copy(1, 1);
        var after = Write(1, 64);

        var taken = TakeOrdered().Select(t => t.Action).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(taken, Is.EqualTo(new PendingResourceAction[] { before, shared, after }));
            Assert.That(_lanes.Count(Buffer(1)), Is.Zero);
        });
    }

    [Test]
    public void EnqueueShared_AcrossKinds_FencesABufferLaneAndATextureLane_AndTakesBothSidesFirst()
    {
        var bufferWrite = Write(1, 0);
        var textureWrite = TextureWrite(1);
        var shared = BufferToTexture(1, 1);
        var afterOnTexture = TextureWrite(1);

        var taken = TakeOrdered().Select(t => t.Action).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(taken, Is.EqualTo(new PendingResourceAction[] { bufferWrite, textureWrite, shared, afterOnTexture }));
            Assert.That(_lanes.Count(Buffer(1)), Is.Zero);
            Assert.That(_lanes.Count(Texture(1)), Is.Zero);
        });
    }

    [Test]
    public void EnqueueShared_TextureToBuffer_TakesTheTextureSideThenTheBufferSideBeforeTheCopy()
    {
        var textureWrite = TextureWrite(1);
        var bufferWrite = Write(1, 0);
        var shared = TextureToBuffer(1, 1);
        var afterOnBuffer = Write(1, 64);

        var taken = TakeOrdered().Select(t => t.Action).ToList();

        Assert.That(taken, Is.EqualTo(new PendingResourceAction[] { textureWrite, bufferWrite, shared, afterOnBuffer }));
    }

    [Test]
    public void Enqueue_OnATextureLane_DoesNotMergeAcrossAFence()
    {
        TextureWrite(1);
        BufferToTexture(1, 1);
        var wholeImage = new PendingTextureWrite(PooledMemory<byte>.CopyFrom(new byte[16]), default, new Extent2D(4, 4),
            new Extent2D(4, 4));

        _lanes.EnqueueTexture(Texture(1), wholeImage);

        Assert.That(_lanes.Count(Texture(1)), Is.EqualTo(3));
    }

    [Test]
    public void TakeOrdered_SpendsTheBudgetOnBothKinds()
    {
        Write(1, 0);
        Write(2, 0);
        TextureWrite(1);
        TextureWrite(2);

        var taken = TakeOrdered(3);

        Assert.Multiple(() =>
        {
            Assert.That(taken, Has.Count.EqualTo(3));
            Assert.That(taken.Count(t => t.Handle.Type == ResourceType.Buffer), Is.EqualTo(2));
            Assert.That(taken.Count(t => t.Handle.Type == ResourceType.Texture), Is.EqualTo(1));
        });
    }

    [Test]
    public void Drop_OfATexture_CancelsTheSharedWriteAndClearsItsFenceFromTheBufferLane()
    {
        var shared = BufferToTexture(1, 1);
        var task = new TaskCompletionSource();
        shared.Tasks.Add(task);

        _lanes.Drop(Texture(1));

        Assert.Multiple(() =>
        {
            Assert.That(task.Task.IsCanceled, Is.True);
            Assert.That(_lanes.Count(Buffer(1)), Is.Zero);
        });
    }

    [Test]
    public void Drop_CancelsTheTasksOfQueuedWrites()
    {
        var write = Write(1, 0);
        var task = new TaskCompletionSource();
        write.Tasks.Add(task);

        _lanes.Drop(Buffer(1));

        Assert.That(task.Task.IsCanceled, Is.True);
    }
}
