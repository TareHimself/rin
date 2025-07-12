using System.Diagnostics;
using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Graphics.FrameGraph;

public class GraphConfig(GraphBuilder builder) : IGraphConfig
{
    public enum ActionType
    {
        Read,
        Write
    }

    public enum DependencyType
    {
        Pass,
        Read,
        Write
    }

    private readonly Dictionary<uint, GraphConfigBuffer> _buffers = [];

    private readonly Dictionary<uint, GraphConfigImage> _images = [];

    public readonly Dictionary<uint, List<Dependency>> PassDependencies = [];
    public readonly Dictionary<uint, List<ResourceAction>> ResourceActions = [];
    public readonly Dictionary<uint, IResourceDescriptor> Resources = [];

    public uint CurrentPassId { get; set; }


    public uint SwapchainImageId { get; set; }

    public uint AddExternalImage(IDeviceImage image, Action? onDispose = null)
    {
        var resourceId = builder.MakeId();
        Resources.Add(resourceId, new ExternalImageResourceDescriptor(new ExternalImage(image, onDispose)));
        return resourceId;
    }

    public uint CreateImage(uint width, uint height, ImageFormat format, ImageLayout layout)
    {
        return CreateImage(new Extent3D(width, height), format, layout);
    }

    public uint CreateImage(in Extent2D extent, ImageFormat format, ImageLayout layout)
    {
        return CreateImage(new Extent3D(extent), format, layout);
    }

    public uint CreateImage(in Extent3D extent, ImageFormat format, ImageLayout layout)
    {
        Debug.Assert(extent is { Width: > 0, Height: > 0, Dimensions: > 0 },"all image dimensions must be greater than zero");
        var flags = format switch
        {
            ImageFormat.Depth => ImageUsage.DepthAttachment,
            ImageFormat.Stencil => ImageUsage.StencilAttachment,
            _ => ImageUsage.None
        };
        var resourceId = builder.MakeId();
        _images.Add(resourceId, new GraphConfigImage
        {
            Extent = extent,
            Usage = DeriveImageUsage(layout) | flags,
            Format = format
        });
        //Resources.Add(resourceId, descriptor); // We do this at the end for images
        // This is always a write because the image was created here
        UseImage(resourceId, layout, ResourceOperation.Write);
        return resourceId;
    }

    public uint CreateBuffer(ulong size, GraphBufferUsage usage)
    {
        Debug.Assert(size > 0,"buffer size must be positive");
        var resourceId = builder.MakeId();
        // _memory.Add(resourceId, descriptor);
        _buffers.Add(resourceId, new GraphConfigBuffer
        {
            Size = size,
            Usage = GraphBufferUsageToVkUsage(usage),
            Mapped = WillUsageRequireMapping(usage)
        });
        UseBuffer(resourceId, usage, ResourceOperation.Write);
        //Write(resourceId);
        return resourceId;
    }

    public uint UseImage(uint id, ImageLayout layout, ResourceOperation operation)
    {
        Debug.Assert(id != 0, "Invalid Image Id");
        {
            var dep = new Dependency
            {
                Type = operation switch
                {
                    ResourceOperation.Write => DependencyType.Write,
                    ResourceOperation.Read => DependencyType.Read,
                    _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
                },
                Id = id
            };

            if (PassDependencies.TryGetValue(CurrentPassId, out var dependencies))
                dependencies.Add(dep);
            else
                PassDependencies.Add(CurrentPassId, [dep]);
        }
        {
            var action = new ResourceAction
            {
                Operation = operation,
                PassId = CurrentPassId,
                Type = ResourceType.Image,
                ImageLayout = layout
            };

            if (ResourceActions.TryGetValue(id, out var passes))
                passes.Add(action);
            else
                ResourceActions.Add(id, [action]);
        }

        {
            if (_images.TryGetValue(id, out var image)) image.Usage |= DeriveImageUsage(layout);
        }

        return id;
    }

    public uint UseBuffer(uint id, GraphBufferUsage usage, ResourceOperation operation)
    {
        Debug.Assert(id != 0, "Invalid Buffer Id");
        {
            var dep = new Dependency
            {
                Type = operation switch
                {
                    ResourceOperation.Write => DependencyType.Write,
                    ResourceOperation.Read => DependencyType.Read,
                    _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
                },
                Id = id
            };

            if (PassDependencies.TryGetValue(CurrentPassId, out var dependencies))
                dependencies.Add(dep);
            else
                PassDependencies.Add(CurrentPassId, [dep]);
        }
        {
            var action = new ResourceAction
            {
                Operation = operation,
                PassId = CurrentPassId,
                Type = ResourceType.Buffer,
                BufferUsage = GraphBufferUsageToBufferUsage(usage)
            };

            if (ResourceActions.TryGetValue(id, out var passes))
                passes.Add(action);
            else
                ResourceActions.Add(id, [action]);
        }

        var configBuffer = _buffers[id];
        configBuffer.Usage |= GraphBufferUsageToVkUsage(usage);
        configBuffer.Mapped = configBuffer.Mapped || WillUsageRequireMapping(usage);
        return id;
    }

    public uint DependOn(uint passId)
    {
        Debug.Assert(passId != 0, "Invalid Pass Id");

        {
            var dep = new Dependency
            {
                Type = DependencyType.Pass,
                Id = passId
            };

            if (PassDependencies.TryGetValue(CurrentPassId, out var dependencies))
                dependencies.Add(dep);
            else
                PassDependencies.Add(CurrentPassId, [dep]);
        }
        return passId;
    }

    private BufferUsage GraphBufferUsageToBufferUsage(GraphBufferUsage usage)
    {
        return usage switch
        {
            GraphBufferUsage.Undefined => BufferUsage.Undefined,
            GraphBufferUsage.Host => BufferUsage.Host,
            GraphBufferUsage.Transfer or GraphBufferUsage.HostThenTransfer => BufferUsage.Transfer,
            GraphBufferUsage.Graphics or GraphBufferUsage.HostThenGraphics => BufferUsage.Graphics,
            GraphBufferUsage.Compute or GraphBufferUsage.HostThenCompute => BufferUsage.Compute,
            GraphBufferUsage.Indirect or GraphBufferUsage.HostThenIndirect => BufferUsage.Indirect,
            _ => throw new ArgumentOutOfRangeException(nameof(usage), usage, null)
        };
    }

    private VkBufferUsageFlags GraphBufferUsageToVkUsage(GraphBufferUsage usage)
    {
        return usage switch
        {
            GraphBufferUsage.Transfer or GraphBufferUsage.HostThenTransfer => VkBufferUsageFlags
                .VK_BUFFER_USAGE_TRANSFER_DST_BIT | VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_SRC_BIT,
            GraphBufferUsage.Indirect or GraphBufferUsage.HostThenIndirect => VkBufferUsageFlags
                .VK_BUFFER_USAGE_INDIRECT_BUFFER_BIT,
            _ => 0
        };
    }

    private bool WillUsageRequireMapping(GraphBufferUsage usage)
    {
        return usage switch
        {
            GraphBufferUsage.Host or GraphBufferUsage.HostThenCompute or GraphBufferUsage.HostThenGraphics
                or GraphBufferUsage.HostThenIndirect or GraphBufferUsage.HostThenTransfer => true,
            _ => false
        };
    }

    private ImageUsage DeriveImageUsage(ImageLayout layout)
    {
        return layout switch
        {
            ImageLayout.Undefined => ImageUsage.None,
            ImageLayout.General or ImageLayout.PresentSrc or ImageLayout.TransferSrc or ImageLayout.TransferDst =>
                ImageUsage.TransferSrc | ImageUsage.TransferDst,
            ImageLayout.ColorAttachment => ImageUsage.ColorAttachment,
            ImageLayout.StencilAttachment => ImageUsage.StencilAttachment,
            ImageLayout.DepthAttachment => ImageUsage.DepthAttachment,
            ImageLayout.ShaderReadOnly => ImageUsage.Sampled,
            _ => throw new ArgumentOutOfRangeException(nameof(layout), layout, null)
        };
    }

    public void FillResources()
    {
        foreach (var (key, image) in _images)
        {
            if (!image.Usage.HasFlag(ImageUsage.ColorAttachment) &&
                !image.Usage.HasFlag(ImageUsage.StencilAttachment) &&
                !image.Usage.HasFlag(ImageUsage.DepthAttachment) && !image.Usage.HasFlag(ImageUsage.Sampled) &&
                !image.Usage.HasFlag(ImageUsage.Storage))
                // We add this because vulkan images require one of the above at minimum
                image.Usage |= ImageUsage.Sampled;
            Resources.Add(key, new ImageResourceDescriptor(image.Extent, image.Format, image.Usage));
        }

        foreach (var (key, buffer) in _buffers)
            Resources.Add(key, new BufferResourceDescriptor(buffer.Size, buffer.Usage, buffer.Mapped));
    }

    public class Dependency
    {
        public DependencyType Type { get; set; }
        public uint Id { get; set; }
    }

    public class ResourceAction
    {
        public required ResourceOperation Operation { get; set; }
        public required uint PassId { get; set; }
        public required ResourceType Type { get; set; }
        public ImageLayout ImageLayout { get; set; }
        public BufferUsage BufferUsage { get; set; }
    }
}