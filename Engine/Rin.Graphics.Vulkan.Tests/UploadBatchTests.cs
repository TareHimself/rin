using Rin.Core.Graphics;
using Rin.Core.Shared;

namespace Rin.Graphics.Vulkan.Tests;

public class UploadBatchTests
{
    private UploadLanes _lanes = null!;
    private AllowList _acquirer = null!;
    private UploadBatch _batch = null!;

    [SetUp]
    public void CreateLanes()
    {
        _lanes = new UploadLanes();
        _acquirer = new AllowList();
        _batch = new UploadBatch();
    }

    [TearDown]
    public void DisposeActions()
    {
        _batch.CompleteWrites();
        _batch.Clear();
        List<PendingResourceAction> remaining = [];
        _lanes.TakeAll(remaining);
        foreach (var action in remaining) action.Dispose();
    }

    private static ResourceHandle Texture(uint id) => new(ResourceType.Texture, id);
    private static ResourceHandle Buffer(uint id) => new(ResourceType.Buffer, id);

    private PendingTextureWrite QueueTexture(uint id, int bytes = 16)
    {
        var write = new PendingTextureWrite(PooledMemory<byte>.CopyFrom(new byte[bytes]), default, new Extent2D(2, 2),
            new Extent2D(2, 2));
        _lanes.EnqueueTexture(Texture(id), write);
        return write;
    }

    private PendingBufferWrite QueueBuffer(uint id, ulong offset, int bytes = 16)
    {
        var write = new PendingBufferWrite(PooledMemory<byte>.CopyFrom(new byte[bytes]), offset);
        _lanes.EnqueueBuffer(Buffer(id), write);
        return write;
    }

    private SharedWrite QueueBufferCopy(uint source, uint destination)
    {
        var shared = new SharedWrite(new BufferCopy(Buffer(source), Buffer(destination), 0, 0, 16));
        _lanes.EnqueueShared(shared);
        return shared;
    }

    private void Fill(int max = 64) => _batch.Fill(_lanes, max, _acquirer);

    [Test]
    public void Fill_TakesAtMostMax_AndLeavesTheRestQueued()
    {
        for (uint id = 1; id <= 3; id++)
        {
            QueueTexture(id);
            QueueBuffer(id, 0);
            _acquirer.Allow(Texture(id));
            _acquirer.Allow(Buffer(id));
        }

        Fill(4);

        Assert.Multiple(() =>
        {
            Assert.That(_batch.Steps, Has.Count.EqualTo(4));
            Assert.That(_lanes.Count(Texture(3)), Is.EqualTo(1));
            Assert.That(_lanes.Count(Buffer(3)), Is.EqualTo(1));
        });
    }

    [Test]
    public void Fill_DisposesTheWriteOfAResourceItCannotAcquire()
    {
        var refused = QueueTexture(1);
        var task = new TaskCompletionSource();
        refused.Tasks.Add(task);
        var allowed = QueueTexture(2);
        _acquirer.Allow(Texture(2));

        Fill();

        Assert.Multiple(() =>
        {
            Assert.That(_batch.Steps.Select(s => s.TextureWrite), Is.EqualTo(new[] { allowed }));
            Assert.That(_batch.Acquired, Is.EqualTo(new[] { Texture(2) }));
            Assert.That(task.Task.IsCanceled, Is.True);
            Assert.That(_acquirer.Asked, Is.EqualTo(new[] { Texture(1), Texture(2) }));
        });
    }

    [Test]
    public void Fill_AcquiresEachResourceOnce_AndGroupsBufferWritesByHandle()
    {
        var first = QueueBuffer(1, 0);
        var second = QueueBuffer(1, 64);
        var other = QueueBuffer(2, 0);
        _acquirer.Allow(Buffer(1));
        _acquirer.Allow(Buffer(2));

        Fill();

        Assert.Multiple(() =>
        {
            Assert.That(_batch.Steps, Has.Count.EqualTo(2));
            Assert.That(_batch.Steps[0].BufferWrites, Is.EqualTo(new[] { first, second }));
            Assert.That(_batch.Steps[1].BufferWrites, Is.EqualTo(new[] { other }));
            Assert.That(_acquirer.Asked, Is.EqualTo(new[] { Buffer(1), Buffer(2) }));
        });
    }

    [Test]
    public void Fill_KeepsTheOrderAcrossResourcesThatACopyConnects()
    {
        var beforeCopy = QueueBuffer(1, 0);
        var copy = QueueBufferCopy(1, 2);
        var intoDestination = QueueBuffer(2, 0);
        var afterCopy = QueueBuffer(1, 64);
        _acquirer.Allow(Buffer(1));
        _acquirer.Allow(Buffer(2));

        Fill();

        Assert.Multiple(() =>
        {
            Assert.That(_batch.Steps.Select(s => s.Kind), Is.EqualTo(new[]
            {
                UploadBatchStepKind.BufferWrites, UploadBatchStepKind.Copy, UploadBatchStepKind.BufferWrites,
                UploadBatchStepKind.BufferWrites
            }));
            Assert.That(_batch.Steps[0].BufferWrites, Is.EqualTo(new[] { beforeCopy }));
            Assert.That(_batch.Steps[1].Shared, Is.SameAs(copy));
            Assert.That(_batch.Steps[2].Handle, Is.EqualTo(Buffer(1)));
            Assert.That(_batch.Steps[2].BufferWrites, Is.EqualTo(new[] { afterCopy }));
            Assert.That(_batch.Steps[3].BufferWrites, Is.EqualTo(new[] { intoDestination }));
        });
    }

    [Test]
    public void Fill_TakesATextureToBufferCopy_WithoutLeavingTheTextureInATransferLayout()
    {
        var copy = new SharedWrite(new TextureToBufferCopy(Texture(1), Buffer(2), default, new Extent2D(2, 2), 0, 16));
        _lanes.EnqueueShared(copy);
        _acquirer.Allow(Texture(1));
        _acquirer.Allow(Buffer(2));

        Fill();

        Assert.Multiple(() =>
        {
            Assert.That(_batch.Steps.Select(s => s.Kind), Is.EqualTo(new[] { UploadBatchStepKind.Copy }));
            Assert.That(_batch.Steps[0].Shared, Is.SameAs(copy));
            Assert.That(_batch.Steps[0].WrittenTexture, Is.EqualTo(default(ResourceHandle)));
            Assert.That(_batch.Acquired, Is.EquivalentTo(new[] { Texture(1), Buffer(2) }));
        });
    }

    [Test]
    public void Fill_TakesACopyWithinOneBufferAfterTheWritesAheadOfIt()
    {
        var before = QueueBuffer(1, 0);
        var copy = QueueBufferCopy(1, 1);
        _acquirer.Allow(Buffer(1));

        Fill();

        Assert.Multiple(() =>
        {
            Assert.That(_batch.Steps.Select(s => s.Kind), Is.EqualTo(new[]
            {
                UploadBatchStepKind.BufferWrites, UploadBatchStepKind.Copy
            }));
            Assert.That(_batch.Steps[0].BufferWrites, Is.EqualTo(new[] { before }));
            Assert.That(_batch.Steps[1].Shared, Is.SameAs(copy));
            Assert.That(_acquirer.Asked, Is.EqualTo(new[] { Buffer(1) }));
        });
    }

    [Test]
    public void Fill_RunsCopiesInEnqueueOrderAcrossLanes()
    {
        var first = QueueBufferCopy(1, 2);
        var second = QueueBufferCopy(3, 1);
        for (uint id = 1; id <= 3; id++) _acquirer.Allow(Buffer(id));

        Fill();

        Assert.That(_batch.Steps.Select(s => s.Shared), Is.EqualTo(new SharedWrite[] { first, second }));
    }

    [Test]
    public void Fill_CancelsOnlyTheCopyWhoseResourceCannotBeAcquired()
    {
        var copy = QueueBufferCopy(1, 2);
        var copyTask = new TaskCompletionSource();
        copy.Tasks.Add(copyTask);
        var other = QueueBufferCopy(3, 4);
        var otherTask = new TaskCompletionSource();
        other.Tasks.Add(otherTask);
        _acquirer.Allow(Buffer(1));
        _acquirer.Allow(Buffer(3));
        _acquirer.Allow(Buffer(4));

        Fill();
        _batch.CompleteWrites();

        Assert.Multiple(() =>
        {
            Assert.That(copyTask.Task.IsCanceled, Is.True);
            Assert.That(otherTask.Task.IsCompletedSuccessfully, Is.True);
            Assert.That(_batch.Steps.Select(s => s.Shared), Is.EqualTo(new[] { other }));
        });
    }

    [Test]
    public void Fill_SumsStagingSizesRoundedToTheAlignment_AndIgnoresCopies()
    {
        QueueTexture(1, 5);
        QueueBuffer(1, 0, 17);
        QueueBufferCopy(1, 2);
        _acquirer.Allow(Texture(1));
        _acquirer.Allow(Buffer(1));
        _acquirer.Allow(Buffer(2));

        Fill();

        Assert.That(_batch.StagingSize, Is.EqualTo(16UL + 32UL));
    }

    [Test]
    public void Fill_WithNothingQueued_IsEmpty()
    {
        Fill();

        Assert.Multiple(() =>
        {
            Assert.That(_batch.IsEmpty, Is.True);
            Assert.That(_batch.StagingSize, Is.Zero);
            Assert.That(_acquirer.Asked, Is.Empty);
        });
    }

    [Test]
    public void CompleteWrites_CompletesEveryTask_AndClearLeavesTheBatchEmpty()
    {
        var textureTask = new TaskCompletionSource();
        var bufferTask = new TaskCompletionSource();
        var copyTask = new TaskCompletionSource();
        QueueTexture(1).Tasks.Add(textureTask);
        QueueBuffer(1, 0).Tasks.Add(bufferTask);
        QueueBufferCopy(1, 2).Tasks.Add(copyTask);
        _acquirer.Allow(Texture(1));
        _acquirer.Allow(Buffer(1));
        _acquirer.Allow(Buffer(2));
        Fill();

        _batch.CompleteWrites();
        _batch.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(textureTask.Task.IsCompletedSuccessfully, Is.True);
            Assert.That(bufferTask.Task.IsCompletedSuccessfully, Is.True);
            Assert.That(copyTask.Task.IsCompletedSuccessfully, Is.True);
            Assert.That(_batch.IsEmpty, Is.True);
            Assert.That(_batch.Acquired, Is.Empty);
        });
    }

    private sealed class AllowList : IResourceAcquirer
    {
        private readonly HashSet<ResourceHandle> _allowed = [];
        public List<ResourceHandle> Asked { get; } = [];

        public void Allow(ResourceHandle handle) => _allowed.Add(handle);

        public bool TryAcquireResource(in ResourceHandle handle)
        {
            Asked.Add(handle);
            return _allowed.Contains(handle);
        }
    }
}
