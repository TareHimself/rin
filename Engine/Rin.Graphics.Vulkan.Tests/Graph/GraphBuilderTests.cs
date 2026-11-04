using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Graphics.Vulkan.Graph;
using Rin.Core.Shared;
using Rin.Graphics.Vulkan.Images;
using TerraFX.Interop.Vulkan;

namespace Rin.Graphics.Vulkan.Tests.Graph;

public class GraphBuilderTests
{
    private GraphBuilder _builder = null!;
    private UploadLanes _lanes = null!;
    private FakeStaging _staging = null!;
    private FakeResources _resources = null!;

    [SetUp]
    public void CreateBuilder()
    {
        _lanes = new UploadLanes();
        _staging = new FakeStaging();
        _resources = new FakeResources();
        _builder = new GraphBuilder(new UnusedPool(), null!, _lanes, _staging, _resources);
    }

    [TearDown]
    public void DisposeQueuedActions()
    {
        List<PendingResourceAction> remaining = [];
        _lanes.TakeAll(remaining);
        foreach (var action in remaining) action.Dispose();
    }

    private static ResourceHandle Buffer(uint id) => new(ResourceType.Buffer, id);

    private PendingBufferWrite QueueWrite(uint id, ulong offset, int bytes = 16)
    {
        var write = new PendingBufferWrite(PooledMemory<byte>.CopyFrom(new byte[bytes]), offset);
        _lanes.EnqueueBuffer(Buffer(id), write);
        return write;
    }

    private SharedWrite QueueCopy(uint source, uint destination)
    {
        var shared = new SharedWrite(new BufferCopy(Buffer(source), Buffer(destination), 0, 0, 16));
        _lanes.EnqueueShared(shared);
        return shared;
    }

    private void ReadBuffer(uint id)
    {
        _builder.AddPass(new BufferReaderPass(Buffer(id)));
    }

    [Test]
    public void Compile_TakesTheWritesOfAResourceTheGraphReads_AndLeavesTheOthersQueued()
    {
        QueueWrite(1, 0);
        QueueWrite(2, 0);
        ReadBuffer(1);

        var graph = _builder.Compile();

        Assert.Multiple(() =>
        {
            Assert.That(graph, Is.Not.Null);
            Assert.That(_lanes.Count(Buffer(1)), Is.Zero);
            Assert.That(_lanes.Count(Buffer(2)), Is.EqualTo(1));
        });
    }

    [Test]
    public void Compile_KeepsACopyNothingReads_WhenALaterWriteOfItsSourceIsKept()
    {
        QueueWrite(1, 0);
        var copy = QueueCopy(1, 2);
        QueueWrite(1, 64);
        ReadBuffer(1);

        _builder.Compile();

        Assert.Multiple(() =>
        {
            Assert.That(copy.Taken, Is.True);
            Assert.That(_lanes.Count(Buffer(1)), Is.Zero);
            Assert.That(_lanes.Count(Buffer(2)), Is.Zero);
        });
    }

    [Test]
    public void Compile_ImportsTheUntouchedSideOfACopy_AndReleasesItWithTheGraph()
    {
        QueueCopy(1, 2);
        ReadBuffer(1);
        _lanes.EnqueueBuffer(Buffer(1), new PendingBufferWrite(PooledMemory<byte>.CopyFrom(new byte[16]), 64));

        var graph = _builder.Compile();
        var importedBeforeDispose = _resources.Acquired.Contains(Buffer(2));
        graph!.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(importedBeforeDispose, Is.True);
            Assert.That(_resources.Released, Is.EquivalentTo(_resources.Acquired));
        });
    }

    [Test]
    public void Compile_LeavesAPrunedSuffixOfALaneQueued_AndTheNextCompileTakesIt()
    {
        QueueWrite(1, 0);
        QueueCopy(1, 2);
        QueueWrite(1, 64);
        var laterWrite = QueueWrite(2, 0);
        ReadBuffer(1);

        _builder.Compile();
        var afterFirst = _lanes.Count(Buffer(2));
        _builder.Reset();
        ReadBuffer(2);
        _builder.Compile();

        Assert.Multiple(() =>
        {
            Assert.That(afterFirst, Is.EqualTo(1));
            Assert.That(_lanes.Count(Buffer(2)), Is.Zero);
            Assert.That(laterWrite.Data.Count, Is.EqualTo(16));
        });
    }

    [Test]
    public void Compile_DoesNotTakeActionsEnqueuedAfterTheyWerePeeked()
    {
        QueueWrite(1, 0);
        QueueCopy(1, 2);
        QueueWrite(1, 64);
        ReadBuffer(1);
        PendingBufferWrite? late = null;
        _resources.OnAcquire = handle =>
        {
            if (handle.Equals(Buffer(2)) && late is null) late = QueueWrite(1, 128);
        };

        _builder.Compile();

        Assert.Multiple(() =>
        {
            Assert.That(late, Is.Not.Null);
            Assert.That(_lanes.Count(Buffer(1)), Is.EqualTo(1));
        });
    }

    [Test]
    public void Compile_AllocatesOneStagingBufferForEveryTakenWrite_AndFreesItWithTheGraph()
    {
        QueueWrite(1, 0, 17);
        QueueWrite(1, 64, 16);
        QueueWrite(1, 128, 16);
        ReadBuffer(1);

        var graph = _builder.Compile();
        var allocated = _staging.Allocated.ToArray();
        var freedBeforeDispose = _staging.Freed.Count;
        graph!.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(allocated, Is.EqualTo(new[] { 32UL + 16UL + 16UL }));
            Assert.That(freedBeforeDispose, Is.Zero);
            Assert.That(_staging.Freed, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void Compile_KeepsAnEarlierCopyOutOfTheSameSource_WhenOnlyALaterCopyIsRead()
    {
        var first = QueueCopy(1, 2);
        var second = QueueCopy(1, 3);
        ReadBuffer(1);
        ReadBuffer(3);

        _builder.Compile();

        Assert.Multiple(() =>
        {
            Assert.That(first.Taken, Is.True);
            Assert.That(second.Taken, Is.True);
            Assert.That(_lanes.Count(Buffer(1)), Is.Zero);
            Assert.That(_lanes.Count(Buffer(2)), Is.Zero);
            Assert.That(_lanes.Count(Buffer(3)), Is.Zero);
        });
    }

    [Test]
    public void Compile_TakesACopyWithinOneBuffer_WithoutItWaitingOnItself()
    {
        QueueWrite(1, 0);
        var copy = QueueCopy(1, 1);
        QueueWrite(1, 64);
        ReadBuffer(1);

        var graph = _builder.Compile();

        Assert.Multiple(() =>
        {
            Assert.That(graph, Is.Not.Null);
            Assert.That(copy.Taken, Is.True);
            Assert.That(_lanes.Count(Buffer(1)), Is.Zero);
        });
    }

    [Test]
    public void Compile_AllocatesNoStaging_ForCopiesAlone()
    {
        QueueCopy(1, 2);
        ReadBuffer(1);

        _builder.Compile();

        Assert.That(_staging.Allocated, Is.Empty);
    }

    private sealed class BufferReaderPass(ResourceHandle buffer) : ITerminalPass
    {
        public uint Id { get; set; }

        public void Configure(IGraphConfig config)
        {
            var id = config.AddExternalBuffer(new DeviceBufferView(buffer, 0, 64));
            config.UseBuffer(id, GraphBufferUsage.Compute, ResourceOperation.Read);
        }

        public void Execute(ICompiledGraph graph, IExecutionContext ctx)
        {
        }
    }

    private sealed class FakeStaging : IStagingAllocator
    {
        public List<ulong> Allocated { get; } = [];
        public List<ResourceHandle> Freed { get; } = [];

        public ResourceHandle Allocate(ulong size)
        {
            Allocated.Add(size);
            return new ResourceHandle(ResourceType.Buffer, 1000 + (uint)Allocated.Count);
        }

        public void Free(in ResourceHandle handle) => Freed.Add(handle);
    }

    private sealed class FakeResources : IExternalResourceSource
    {
        public List<ResourceHandle> Acquired { get; } = [];
        public List<ResourceHandle> Released { get; } = [];
        public Action<ResourceHandle>? OnAcquire { get; set; }

        public bool TryAcquireResource(in ResourceHandle handle)
        {
            Acquired.Add(handle);
            OnAcquire?.Invoke(handle);
            return true;
        }

        public void ReleaseResource(in ResourceHandle handle) => Released.Add(handle);
        public IVulkanTexture? GetTexture(in ResourceHandle handle) => null;
        public IVulkanCubemap? GetCubemap(in ResourceHandle handle) => null;
        public IVulkanTextureArray? GetTextureArray(in ResourceHandle handle) => null;
        public IVulkanDeviceBuffer? ResolveBuffer(in ResourceHandle handle) => new FakeBuffer(handle);
    }

    private sealed class FakeBuffer(ResourceHandle handle) : IVulkanDeviceBuffer
    {
        public ulong Offset => 0;
        public ulong Size => 1024;
        public ResourceHandle Handle => handle;
        public VkBuffer NativeBuffer => default;
        public IntPtr Allocation => IntPtr.Zero;
        public BufferGraphState? GraphState { get; set; }
        public ulong GetAddress() => 0;
        public DeviceBufferView GetView(ulong offset, ulong size) => new(handle, offset, size);
        public void WriteRaw(in IntPtr src, ulong size, ulong offset = 0)
        {
        }

        public void Dispose()
        {
        }
    }

    [Test]
    public void Compile_KeepsAPassThatWritesATerminalResource()
    {
        var writer = new ImagePass(GraphResourceFlags.Terminal);
        _builder.AddPass(writer);

        var graph = _builder.Compile();

        Assert.Multiple(() =>
        {
            Assert.That(graph, Is.Not.Null);
            Assert.That(writer.Disposed, Is.False);
        });
    }

    [Test]
    public void Compile_KeepsWhatATerminalWriterReads_AndPrunesTheRest()
    {
        var source = new ImagePass();
        var terminal = new ImagePass(GraphResourceFlags.Terminal, source);
        var unused = new ImagePass();
        _builder.AddPass(source);
        _builder.AddPass(terminal);
        _builder.AddPass(unused);

        var graph = _builder.Compile();

        Assert.Multiple(() =>
        {
            Assert.That(graph, Is.Not.Null);
            Assert.That(source.Disposed, Is.False);
            Assert.That(terminal.Disposed, Is.False);
            Assert.That(unused.Disposed, Is.True);
        });
    }

    [Test]
    public void Compile_ReturnsNull_WhenNothingIsTerminal()
    {
        var pass = new ImagePass();
        _builder.AddPass(pass);

        var graph = _builder.Compile();

        Assert.Multiple(() =>
        {
            Assert.That(graph, Is.Null);
            Assert.That(pass.Disposed, Is.True);
        });
    }

    [Test]
    public void Compile_StillRootsFromTerminalPasses()
    {
        var source = new ImagePass();
        _builder.AddPass(source);
        _builder.AddPass(new ReadingTerminalPass(source));

        var graph = _builder.Compile();

        Assert.Multiple(() =>
        {
            Assert.That(graph, Is.Not.Null);
            Assert.That(source.Disposed, Is.False);
        });
    }

    private sealed class ImagePass(GraphResourceFlags flags = GraphResourceFlags.None, ImagePass? reads = null)
        : IPass, IDisposable
    {
        public uint ImageId { get; private set; }
        public bool Disposed { get; private set; }
        public uint Id { get; set; }

        public void Configure(IGraphConfig config)
        {
            if (reads != null) config.ReadTexture(reads.ImageId, ImageLayout.ShaderReadOnly);
            ImageId = config.CreateTexture(new Extent2D(4, 4), ImageFormat.R8, ImageLayout.ColorAttachment, flags);
        }

        public void Execute(ICompiledGraph graph, IExecutionContext ctx)
        {
        }

        public void Dispose()
        {
            Disposed = true;
        }
    }

    private sealed class ReadingTerminalPass(ImagePass reads) : ITerminalPass
    {
        public uint Id { get; set; }

        public void Configure(IGraphConfig config)
        {
            config.ReadTexture(reads.ImageId, ImageLayout.ShaderReadOnly);
        }

        public void Execute(ICompiledGraph graph, IExecutionContext ctx)
        {
        }
    }

    private sealed class UnusedPool : IResourcePool
    {
        public IDisposableVulkanTexture CreateTexture(TextureResourceDescriptor descriptor, Frame frame)
        {
            throw new NotSupportedException();
        }

        public IDisposableVulkanTextureArray CreateTextureArray(TextureArrayResourceDescriptor descriptor, Frame frame)
        {
            throw new NotSupportedException();
        }

        public IDisposableVulkanCubemap CreateCubemap(CubemapResourceDescriptor descriptor, Frame frame)
        {
            throw new NotSupportedException();
        }

        public IVulkanDeviceBuffer CreateBuffer(BufferResourceDescriptor descriptor, Frame frame)
        {
            throw new NotSupportedException();
        }

        public void OnFrameEnd(ulong newFrame)
        {
        }

        public void Dispose()
        {
        }
    }
}
