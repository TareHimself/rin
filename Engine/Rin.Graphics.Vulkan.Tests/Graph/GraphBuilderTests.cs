using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Graphics.Vulkan.Graph;
using Rin.Graphics.Vulkan.Images;

namespace Rin.Graphics.Vulkan.Tests.Graph;

public class GraphBuilderTests
{
    private GraphBuilder _builder = null!;

    [SetUp]
    public void CreateBuilder()
    {
        _builder = new GraphBuilder(new UnusedPool(), null!);
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
