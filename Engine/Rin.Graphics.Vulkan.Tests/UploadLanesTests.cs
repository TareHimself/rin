using Rin.Core.Graphics;
using Rin.Core.Shared;

namespace Rin.Graphics.Vulkan.Tests;

public class UploadLanesTests : UploadLanesFixture
{
    private PendingTextureWrite TextureWrite(uint id)
    {
        var write = new PendingTextureWrite(PooledMemory<byte>.CopyFrom(new byte[16]),
            ImageRegion.Whole(new Extent2D(2, 2)), new ImageShape(new Extent2D(4, 4)));
        Lanes.EnqueueTexture(Texture(id), write);
        return write;
    }

    private SharedWrite BufferToTexture(uint buffer, uint texture)
    {
        var shared = new SharedWrite(new BufferToTextureCopy(Buffer(buffer), Texture(texture), 0, 16, default,
            new Extent2D(2, 2)));
        Lanes.EnqueueShared(shared);
        return shared;
    }

    private SharedWrite TextureToBuffer(uint texture, uint buffer)
    {
        var shared = new SharedWrite(new TextureToBufferCopy(Texture(texture), Buffer(buffer), default,
            new Extent2D(2, 2), 0, 16));
        Lanes.EnqueueShared(shared);
        return shared;
    }

    private List<(ResourceHandle Handle, PendingResourceAction Action)> TakeOrdered(int max = 64)
    {
        List<(ResourceHandle, PendingResourceAction)> taken = [];
        Lanes.TakeOrdered(max, taken);
        return taken;
    }

    [Test]
    public void Enqueue_MergesACoveringWriteIntoThePreviousOne_AndAdoptsItsTasks()
    {
        var covered = QueueWrite(1, 16);
        var task = new TaskCompletionSource();
        covered.Tasks.Add(task);

        var covering = QueueWrite(1, 0, 64);

        Assert.Multiple(() =>
        {
            Assert.That(Lanes.Count(Buffer(1)), Is.EqualTo(1));
            Assert.That(covering.Tasks, Is.EqualTo(new[] { task }));
        });
    }

    [Test]
    public void Enqueue_DoesNotMergeAcrossAFence()
    {
        QueueWrite(1, 16);
        QueueCopy(1, 2);

        QueueWrite(1, 0, 64);

        Assert.That(Lanes.Count(Buffer(1)), Is.EqualTo(3));
    }

    [Test]
    public void Enqueue_DoesNotMergeIntoItemsThatWerePeeked()
    {
        var peeked = QueueWrite(1, 16);
        List<PendingResourceAction> items = [];
        Lanes.Peek(Buffer(1), items);

        QueueWrite(1, 0, 64);
        List<PendingResourceAction> taken = [];
        Lanes.Take(Buffer(1), items.Count, taken);

        Assert.Multiple(() =>
        {
            Assert.That(taken, Is.EqualTo(new[] { peeked }));
            Assert.That(Lanes.Count(Buffer(1)), Is.EqualTo(1));
        });
    }

    [Test]
    public void EnqueueCopy_PutsTheSameFenceInBothLanes_AndOnceInTheCopyList()
    {
        var copy = QueueCopy(1, 2);

        List<PendingResourceAction> source = [];
        List<PendingResourceAction> destination = [];
        List<SharedWrite> copies = [];
        Lanes.Peek(Buffer(1), source);
        Lanes.Peek(Buffer(2), destination);
        Lanes.PeekShared(copies);

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
        var intoB = QueueWrite(2, 0);
        var copy1 = QueueCopy(1, 2);
        var afterCopyOnA = QueueWrite(1, 0);
        var afterCopyOnB = QueueWrite(2, 16);
        var copy2 = QueueCopy(2, 3);

        var taken = TakeOrdered().Select(t => t.Action).ToList();

        Assert.That(taken, Is.EqualTo(new PendingResourceAction[] { intoB, copy1, afterCopyOnB, copy2, afterCopyOnA }));
    }

    [Test]
    public void TakeOrdered_StopsAtACopyThatDoesNotFit_AndLeavesItsLanesBlockedAtTheFence()
    {
        var firstWrite = QueueWrite(1, 0);
        var firstCopy = QueueCopy(1, 2);
        var secondWrite = QueueWrite(3, 0);
        var secondCopy = QueueCopy(3, 4);
        var behindFence = QueueWrite(3, 16);

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
        QueueWrite(1, 0);
        QueueWrite(1, 16);
        var copy = QueueCopy(1, 2);

        var taken = TakeOrdered(1);

        Assert.That(taken, Has.Count.EqualTo(3));
        Assert.That(taken[^1].Action, Is.SameAs(copy));
    }

    [Test]
    public void Take_OfACopyFromOneLane_RemovesItFromTheCopyList()
    {
        var copy = QueueCopy(1, 2);
        List<PendingResourceAction> taken = [];
        List<SharedWrite> copies = [];

        Lanes.Take(Buffer(1), 1, taken);
        Lanes.PeekShared(copies);

        Assert.Multiple(() =>
        {
            Assert.That(taken, Is.EqualTo(new[] { copy }));
            Assert.That(copy.Taken, Is.True);
            Assert.That(copies, Is.Empty);
            Assert.That(Lanes.Count(Buffer(2)), Is.EqualTo(1));
        });
    }

    [Test]
    public void Drop_CancelsCopiesThatReferenceTheResource_AndRemovesTheirFenceFromTheOtherLane()
    {
        var copy = QueueCopy(1, 2);
        var task = new TaskCompletionSource();
        copy.Tasks.Add(task);
        var write = QueueWrite(2, 0);
        List<SharedWrite> copies = [];

        Lanes.Drop(Buffer(1));
        Lanes.PeekShared(copies);

        Assert.Multiple(() =>
        {
            Assert.That(task.Task.IsCanceled, Is.True);
            Assert.That(copies, Is.Empty);
            Assert.That(Lanes.Count(Buffer(1)), Is.Zero);
            Assert.That(Lanes.Count(Buffer(2)), Is.EqualTo(1));
            Assert.That(TakeOrdered().Select(t => t.Action), Is.EqualTo(new PendingResourceAction[] { write }));
        });
    }

    [Test]
    public void EnqueueShared_OnASingleLane_PutsExactlyOneFence_AndTakesWithoutWaitingOnItself()
    {
        var before = QueueWrite(1, 0);
        var shared = QueueCopy(1, 1);
        var after = QueueWrite(1, 64);

        var taken = TakeOrdered().Select(t => t.Action).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(taken, Is.EqualTo(new PendingResourceAction[] { before, shared, after }));
            Assert.That(Lanes.Count(Buffer(1)), Is.Zero);
        });
    }

    [Test]
    public void EnqueueShared_AcrossKinds_FencesABufferLaneAndATextureLane_AndTakesBothSidesFirst()
    {
        var bufferWrite = QueueWrite(1, 0);
        var textureWrite = TextureWrite(1);
        var shared = BufferToTexture(1, 1);
        var afterOnTexture = TextureWrite(1);

        var taken = TakeOrdered().Select(t => t.Action).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(taken, Is.EqualTo(new PendingResourceAction[] { bufferWrite, textureWrite, shared, afterOnTexture }));
            Assert.That(Lanes.Count(Buffer(1)), Is.Zero);
            Assert.That(Lanes.Count(Texture(1)), Is.Zero);
        });
    }

    [Test]
    public void EnqueueShared_TextureToBuffer_TakesTheTextureSideThenTheBufferSideBeforeTheCopy()
    {
        var textureWrite = TextureWrite(1);
        var bufferWrite = QueueWrite(1, 0);
        var shared = TextureToBuffer(1, 1);
        var afterOnBuffer = QueueWrite(1, 64);

        var taken = TakeOrdered().Select(t => t.Action).ToList();

        Assert.That(taken, Is.EqualTo(new PendingResourceAction[] { textureWrite, bufferWrite, shared, afterOnBuffer }));
    }

    [Test]
    public void Enqueue_OnATextureLane_DoesNotMergeAcrossAFence()
    {
        TextureWrite(1);
        BufferToTexture(1, 1);
        var wholeImage = new PendingTextureWrite(PooledMemory<byte>.CopyFrom(new byte[16]),
            ImageRegion.Whole(new Extent2D(4, 4)), new ImageShape(new Extent2D(4, 4)));

        Lanes.EnqueueTexture(Texture(1), wholeImage);

        Assert.That(Lanes.Count(Texture(1)), Is.EqualTo(3));
    }

    [Test]
    public void TakeOrdered_SpendsTheBudgetOnBothKinds()
    {
        QueueWrite(1, 0);
        QueueWrite(2, 0);
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

        Lanes.Drop(Texture(1));

        Assert.Multiple(() =>
        {
            Assert.That(task.Task.IsCanceled, Is.True);
            Assert.That(Lanes.Count(Buffer(1)), Is.Zero);
        });
    }

    [Test]
    public void Drop_CancelsTheTasksOfQueuedWrites()
    {
        var write = QueueWrite(1, 0);
        var task = new TaskCompletionSource();
        write.Tasks.Add(task);

        Lanes.Drop(Buffer(1));

        Assert.That(task.Task.IsCanceled, Is.True);
    }
}
