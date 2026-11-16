using System.Reflection;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Graphics.Vulkan.Graph;
using Rin.Core.Shared;
using Rin.Graphics.Vulkan.Images;
using TerraFX.Interop.Vulkan;

namespace Rin.Graphics.Vulkan.Tests.Graph;

public class GraphBuilderTests : UploadLanesFixture
{
    private GraphBuilder _builder = null!;
    private FakeStaging _staging = null!;
    private FakeResources _resources = null!;

    [SetUp]
    public void CreateBuilder()
    {
        _staging = new FakeStaging();
        _resources = new FakeResources();
        _builder = new GraphBuilder(new UnusedPool(), null!, Lanes, _staging, _resources);
    }

    private void ReadBuffer(uint id)
    {
        _builder.AddPass(new BufferReaderPass(Buffer(id)));
    }

    private void QueueImageWrite(in ResourceHandle image)
    {
        var extent = new Extent2D(2, 2);
        Lanes.EnqueueTexture(image, new PendingTextureWrite(PooledMemory<byte>.CopyFrom(new byte[16]),
            ImageRegion.Whole(extent), new ImageShape(extent, 1, 6)));
    }

    [TestCase(ResourceType.Texture)]
    [TestCase(ResourceType.TextureArray)]
    [TestCase(ResourceType.Cubemap)]
    public void CompileTakesTheWritesOfAnImageTheGraphReadsWhateverItsKind(ResourceType type)
    {
        var read = new ResourceHandle(type, 1);
        var unread = new ResourceHandle(type, 2);
        QueueImageWrite(read);
        QueueImageWrite(unread);
        _builder.AddPass(new ImageReaderPass(read));

        var graph = _builder.Compile();

        Assert.Multiple(() =>
        {
            Assert.That(graph, Is.Not.Null);
            Assert.That(Lanes.Count(read), Is.Zero);
            Assert.That(Lanes.Count(unread), Is.EqualTo(1));
            Assert.That(_staging.Allocated, Is.EqualTo(new[] { 16ul }));
        });
    }

    [Test]
    public void CompileTakesTheWritesOfAResourceTheGraphReadsAndLeavesTheOthersQueued()
    {
        QueueWrite(1, 0);
        QueueWrite(2, 0);
        ReadBuffer(1);

        var graph = _builder.Compile();

        Assert.Multiple(() =>
        {
            Assert.That(graph, Is.Not.Null);
            Assert.That(Lanes.Count(Buffer(1)), Is.Zero);
            Assert.That(Lanes.Count(Buffer(2)), Is.EqualTo(1));
        });
    }

    [Test]
    public void CompileKeepsACopyNothingReadsWhenALaterWriteOfItsSourceIsKept()
    {
        QueueWrite(1, 0);
        var copy = QueueCopy(1, 2);
        QueueWrite(1, 64);
        ReadBuffer(1);

        _builder.Compile();

        Assert.Multiple(() =>
        {
            Assert.That(copy.Taken, Is.True);
            Assert.That(Lanes.Count(Buffer(1)), Is.Zero);
            Assert.That(Lanes.Count(Buffer(2)), Is.Zero);
        });
    }

    [Test]
    public void CompileImportsTheUntouchedSideOfACopyAndReleasesItWithTheGraph()
    {
        QueueCopy(1, 2);
        ReadBuffer(1);
        Lanes.EnqueueBuffer(Buffer(1), new PendingBufferWrite(PooledMemory<byte>.CopyFrom(new byte[16]), 64));

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
    public void CompileLeavesAPrunedSuffixOfALaneQueuedAndTheNextCompileTakesIt()
    {
        QueueWrite(1, 0);
        QueueCopy(1, 2);
        QueueWrite(1, 64);
        var laterWrite = QueueWrite(2, 0);
        ReadBuffer(1);

        _builder.Compile();
        var afterFirst = Lanes.Count(Buffer(2));
        _builder.Reset();
        ReadBuffer(2);
        _builder.Compile();

        Assert.Multiple(() =>
        {
            Assert.That(afterFirst, Is.EqualTo(1));
            Assert.That(Lanes.Count(Buffer(2)), Is.Zero);
            Assert.That(laterWrite.Data.Count, Is.EqualTo(16));
        });
    }

    [Test]
    public void CompileDoesNotTakeActionsEnqueuedAfterTheyWerePeeked()
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
            Assert.That(Lanes.Count(Buffer(1)), Is.EqualTo(1));
        });
    }

    [Test]
    public void CompileAllocatesOneStagingBufferForEveryTakenWriteAndFreesItWithTheGraph()
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
    public void CompileKeepsAnEarlierCopyOutOfTheSameSourceWhenOnlyALaterCopyIsRead()
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
            Assert.That(Lanes.Count(Buffer(1)), Is.Zero);
            Assert.That(Lanes.Count(Buffer(2)), Is.Zero);
            Assert.That(Lanes.Count(Buffer(3)), Is.Zero);
        });
    }

    [Test]
    public void CompileTakesACopyWithinOneBufferWithoutItWaitingOnItself()
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
            Assert.That(Lanes.Count(Buffer(1)), Is.Zero);
        });
    }

    [Test]
    public void CompileAllocatesNoStagingForCopiesAlone()
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

    private sealed class ImageReaderPass(ResourceHandle image) : ITerminalPass
    {
        public uint Id { get; set; }

        public void Configure(IGraphConfig config)
        {
            var id = config.AddExternalImage(image);
            config.UseTexture(id, ImageLayout.ShaderReadOnly, ResourceOperation.Read);
        }

        public void Execute(ICompiledGraph graph, IExecutionContext ctx)
        {
        }
    }

    private sealed class FakeImage(ResourceHandle handle) : IVulkanTexture, IVulkanCubemap, IVulkanTextureArray
    {
        public Extent2D Extent => new(2, 2);
        public uint MipLevels => 1;
        public uint LayerCount => 6;
        public uint Count => 6;
        public ImageCreateFlags Usage => ImageCreateFlags.Sampled | ImageCreateFlags.TransferDst;
        public ImageFormat Format => ImageFormat.RGBA8;
        public ResourceHandle Handle => handle;
        public VkImage VulkanImage => default;
        public VkImageView VulkanView => default;
        public ImageLayout Layout { get; set; }
        public IntPtr Allocation => IntPtr.Zero;
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
        public IVulkanTexture? GetTexture(in ResourceHandle handle) => new FakeImage(handle);
        public IVulkanCubemap? GetCubemap(in ResourceHandle handle) => new FakeImage(handle);
        public IVulkanTextureArray? GetTextureArray(in ResourceHandle handle) => new FakeImage(handle);
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
        public DeviceBufferView GetView(ulong offset, ulong size) => new(handle, offset, size);
        public void WriteRaw(in IntPtr src, ulong size, ulong offset = 0)
        {
        }

        public void Dispose()
        {
        }
    }

    [Test]
    public void CompileKeepsAPassThatWritesATerminalResource()
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
    public void CompileKeepsWhatATerminalWriterReadsAndPrunesTheRest()
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
    public void CompileReturnsNullWhenNothingIsTerminal()
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
    public void CompileStillRootsFromTerminalPasses()
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

    [Test]
    public void CompileAfterResetMatchesAFreshBuilder()
    {
        AddChain(_builder, 2);
        _builder.Compile()!.Dispose();
        _builder.Reset();
        AddChain(_builder, 3);
        var reused = Describe(_builder.Compile()!);

        var fresh = new GraphBuilder(new UnusedPool(), null!, new UploadLanes(), _staging, _resources);
        AddChain(fresh, 3);

        Assert.That(reused, Is.EqualTo(Describe(fresh.Compile()!)));
    }

    [Test]
    public void CompileReusesADisposedGraph()
    {
        AddChain(_builder, 2);
        var first = _builder.Compile()!;
        first.Dispose();
        _builder.Reset();
        AddChain(_builder, 2);

        Assert.That(_builder.Compile(), Is.SameAs(first));
    }

    [Test]
    public void CompileLeavesAGraphThatIsStillInUseAlone()
    {
        AddChain(_builder, 2);
        var first = (CompiledGraph)_builder.Compile()!;
        var before = Describe(first);
        _builder.Reset();
        AddChain(_builder, 3);

        var second = _builder.Compile();

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(Describe(first), Is.EqualTo(before));
        });
    }

    [Test]
    public void DisposeTwiceDisposesItsPassesOnce()
    {
        var pass = new ImagePass(GraphResourceFlags.Terminal);
        _builder.AddPass(pass);
        var graph = _builder.Compile()!;

        graph.Dispose();
        graph.Dispose();

        Assert.That(pass.Disposals, Is.EqualTo(1));
    }

    /// <summary>
    ///     The render graph overlay example reads these private fields by name.
    /// </summary>
    [Test]
    public void TheFieldsTheOverlayReadsByReflectionKeepTheirNamesAndShapes()
    {
        AddChain(_builder, 2);
        var graph = _builder.Compile()!;

        Assert.Multiple(() =>
        {
            Assert.That(ReadField(graph, "_nodes"), Is.InstanceOf<IEnumerable<ExecutionGroup>>());
            Assert.That(ReadField(graph, "_descriptors"), Is.InstanceOf<IDictionary<uint, IResourceDescriptor>>());
            Assert.That(ReadField(_builder, "_passes"), Is.InstanceOf<IDictionary<uint, IPass>>());
        });
    }

    private static object? ReadField(object target, string name)
    {
        return target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target);
    }

    /// <summary>
    ///     Adds passes that each read the image the one before wrote, ending in a terminal write.
    /// </summary>
    private static void AddChain(GraphBuilder builder, int length)
    {
        ImagePass? previous = null;
        for (var i = 0; i < length; i++)
        {
            var flags = i == length - 1 ? GraphResourceFlags.Terminal : GraphResourceFlags.None;
            previous = new ImagePass(flags, previous);
            builder.AddPass(previous);
        }
    }

    private static List<string> Describe(ICompiledGraph graph)
    {
        List<string> lines = [];
        foreach (var group in ((CompiledGraph)graph).Groups)
        foreach (var pass in group.Passes)
            if (pass is BarrierPass barrier)
                lines.Add($"barrier {pass.Id}: {string.Join(", ", barrier.Buffers)} | {string.Join(", ", barrier.Images)}");
            else
                lines.Add($"{pass.GetType().Name} {pass.Id}");
        return lines;
    }

    private sealed class ImagePass(GraphResourceFlags flags = GraphResourceFlags.None, ImagePass? reads = null)
        : IPass, IDisposable
    {
        public uint ImageId { get; private set; }
        public bool Disposed => Disposals > 0;
        public int Disposals { get; private set; }
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
            Disposals++;
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
