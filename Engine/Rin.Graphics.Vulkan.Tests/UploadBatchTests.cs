using Rin.Core.Graphics;
using Rin.Core.Shared;

namespace Rin.Graphics.Vulkan.Tests;

public class UploadBatchTests
{
    private TextureWriteLanes _textures = null!;
    private BufferWriteLanes _buffers = null!;
    private AllowList _acquirer = null!;
    private UploadBatch _batch = null!;

    [SetUp]
    public void CreateLanes()
    {
        _textures = new TextureWriteLanes();
        _buffers = new BufferWriteLanes();
        _acquirer = new AllowList();
        _batch = new UploadBatch();
    }

    [TearDown]
    public void DisposeWrites()
    {
        _batch.CompleteWrites();
        _batch.Clear();
        List<(ResourceHandle, PendingTextureWrite)> textures = [];
        _textures.TakeAll(textures);
        foreach (var (_, write) in textures) write.Dispose();
        List<(ResourceHandle, PendingBufferWrite)> buffers = [];
        _buffers.TakeAll(buffers);
        foreach (var (_, write) in buffers) write.Dispose();
    }

    private static ResourceHandle Texture(uint id) => new(ResourceType.Texture, id);
    private static ResourceHandle Buffer(uint id) => new(ResourceType.Buffer, id);

    private PendingTextureWrite QueueTexture(uint id, int bytes = 16)
    {
        var write = new PendingTextureWrite(PooledMemory<byte>.CopyFrom(new byte[bytes]), default, new Extent2D(2, 2),
            new Extent2D(2, 2));
        _textures.Enqueue(Texture(id), write);
        return write;
    }

    private PendingBufferWrite QueueBuffer(uint id, ulong offset, int bytes = 16)
    {
        var write = new PendingBufferWrite(PooledMemory<byte>.CopyFrom(new byte[bytes]), offset);
        _buffers.Enqueue(Buffer(id), write);
        return write;
    }

    private void Fill(int maxPerKind = 64) => _batch.Fill(_textures, _buffers, maxPerKind, _acquirer);

    [Test]
    public void Fill_TakesAtMostMaxPerKind_AndLeavesTheRestQueued()
    {
        for (uint id = 1; id <= 3; id++)
        {
            QueueTexture(id);
            QueueBuffer(id, 0);
            _acquirer.Allow(Texture(id));
            _acquirer.Allow(Buffer(id));
        }

        Fill(2);

        Assert.Multiple(() =>
        {
            Assert.That(_batch.Textures, Has.Count.EqualTo(2));
            Assert.That(_batch.Buffers, Has.Count.EqualTo(2));
            Assert.That(_textures.Peek(Texture(3), out _), Is.EqualTo(1));
            Assert.That(_buffers.Peek(Buffer(3), out _), Is.EqualTo(1));
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
            Assert.That(_batch.Textures.Select(t => t.Write), Is.EqualTo(new[] { allowed }));
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
            Assert.That(_batch.Buffers[Buffer(1)], Is.EqualTo(new[] { first, second }));
            Assert.That(_batch.Buffers[Buffer(2)], Is.EqualTo(new[] { other }));
            Assert.That(_acquirer.Asked, Is.EqualTo(new[] { Buffer(1), Buffer(2) }));
        });
    }

    [Test]
    public void Fill_SumsStagingSizesRoundedToTheAlignment()
    {
        QueueTexture(1, 5);
        QueueBuffer(1, 0, 17);
        _acquirer.Allow(Texture(1));
        _acquirer.Allow(Buffer(1));

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
        QueueTexture(1).Tasks.Add(textureTask);
        QueueBuffer(1, 0).Tasks.Add(bufferTask);
        _acquirer.Allow(Texture(1));
        _acquirer.Allow(Buffer(1));
        Fill();

        _batch.CompleteWrites();
        _batch.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(textureTask.Task.IsCompletedSuccessfully, Is.True);
            Assert.That(bufferTask.Task.IsCompletedSuccessfully, Is.True);
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
