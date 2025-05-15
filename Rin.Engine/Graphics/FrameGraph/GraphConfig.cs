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

    public readonly Dictionary<uint, List<Dependency>> PassDependencies = [];
    public readonly Dictionary<uint, List<ResourceAction>> ResourceActions = [];
    public readonly Dictionary<uint, IResourceDescriptor> Resources = [];
    private readonly Dictionary<uint, GraphConfigImage> _images = [];

    public uint CurrentPassId { get; set; }


    public uint SwapchainImageId { get; set; }

    public uint AddExternalImage(IDeviceImage image, Action? onDispose = null)
    {
        var resourceId = builder.MakeId();
        Resources.Add(resourceId, new ExternalImageResourceDescriptor(new ExternalImage(image, onDispose)));
        return resourceId;
    }

    private ImageUsage DeriveImageUsage(ImageLayout layout) => layout switch
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

    public uint CreateImage(uint width, uint height, ImageFormat format, ImageLayout layout)
    {
        return CreateImage(new Extent3D(width,height), format, layout);
    }

    public uint CreateImage(in Extent2D extent, ImageFormat format, ImageLayout layout)
    {
        return CreateImage(new Extent3D(extent), format, layout);
    }

    public uint CreateImage(in Extent3D extent, ImageFormat format, ImageLayout layout)
    {
        var flags = format switch
        {
            ImageFormat.Depth => ImageUsage.DepthAttachment,
            ImageFormat.Stencil => ImageUsage.StencilAttachment,
            _ => ImageUsage.None
        };
        var resourceId = builder.MakeId();
        _images.Add(resourceId,new GraphConfigImage
        {
            Extent = extent,
            Usage = DeriveImageUsage(layout) | flags,
            Format = format
        });
        //Resources.Add(resourceId, descriptor); // We do this at the end for images
        // This is always a write because the image was created here
        UseImage(resourceId,layout, ResourceUsage.Write);
        return resourceId;
    }

    public uint CreateBuffer(ulong size, BufferStage stage)
    {
        var descriptor = new BufferResourceDescriptor(size);
        var resourceId = builder.MakeId();
        // _memory.Add(resourceId, descriptor);
        Resources.Add(resourceId, descriptor);
        UseBuffer(resourceId, stage, ResourceUsage.Write);
        //Write(resourceId);
        return resourceId;
    }

    public uint UseImage(uint id, ImageLayout layout, ResourceUsage usage)
    {
        {
            var dep = new Dependency
            {
                Type = usage switch
                {
                    ResourceUsage.Write => DependencyType.Write,
                    ResourceUsage.Read => DependencyType.Read,
                    _ => throw new ArgumentOutOfRangeException(nameof(usage), usage, null)
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
                Usage = usage,
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
            if (_images.TryGetValue(id, out var image))
            {
                image.Usage |= DeriveImageUsage(layout);
            }
        }
        
        return id;
    }

    public uint UseBuffer(uint id, BufferStage stage, ResourceUsage usage)
    {
        {
            var dep = new Dependency
            {
                Type = usage switch
                {
                    ResourceUsage.Write => DependencyType.Write,
                    ResourceUsage.Read => DependencyType.Read,
                    _ => throw new ArgumentOutOfRangeException(nameof(usage), usage, null)
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
                Usage = usage,
                PassId = CurrentPassId,
                Type = ResourceType.Buffer,
                BufferStage = stage
            };

            if (ResourceActions.TryGetValue(id, out var passes))
                passes.Add(action);
            else
                ResourceActions.Add(id, [action]);
        }
        return id;
    }

    public uint DependOn(uint passId)
    {
        if (passId == 0) throw new Exception();
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

    public void FillImageResources()
    {
        foreach (var (key, image) in _images)
        {
            Resources.Add(key,new ImageResourceDescriptor(image.Extent,image.Format,image.Usage));
        }
    }

    public class Dependency
    {
        public DependencyType Type { get; set; }
        public uint Id { get; set; }
    }

    public class ResourceAction
    {
        public required ResourceUsage Usage { get; set; }
        public required uint PassId { get; set; }
        public required ResourceType Type { get; set; }
        public ImageLayout ImageLayout { get; set; }
        public BufferStage BufferStage { get; set; }
    }
}