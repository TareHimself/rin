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

    public readonly Dictionary<uint, List<Dependency>> PassDependencies = [];
    public readonly Dictionary<uint, List<ResourceAction>> ResourceActions = [];
    public readonly Dictionary<uint, IResourceDescriptor> Resources = [];

    public uint CurrentPassId { get; set; }


    public uint CreateImage(uint width, uint height, ImageFormat format, ImageLayout initialLayout)
    {
        var flags = format is ImageFormat.Depth or ImageFormat.Stencil
            ? VkImageUsageFlags.VK_IMAGE_USAGE_DEPTH_STENCIL_ATTACHMENT_BIT
            : VkImageUsageFlags.VK_IMAGE_USAGE_STORAGE_BIT | VkImageUsageFlags.VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT;

        flags |= VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_SRC_BIT | VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_DST_BIT |
                 VkImageUsageFlags.VK_IMAGE_USAGE_SAMPLED_BIT;
        var descriptor = new ImageResourceDescriptor(width, height, format, flags, initialLayout);
        var resourceId = builder.MakeId();
        Resources.Add(resourceId, descriptor);
        UseImage(resourceId, ImageLayout.Undefined, ResourceUsage.Write);
        return resourceId;
    }

    public uint CreateImage(in Extent2D extent, ImageFormat format, ImageLayout initialLayout = ImageLayout.Undefined)
    {
        return CreateImage(extent.Width, extent.Height, format, initialLayout);
    }

    public uint AllocateBuffer(ulong size)
    {
        var descriptor = new BufferResourceDescriptor(size);
        var resourceId = builder.MakeId();
        // _memory.Add(resourceId, descriptor);
        Resources.Add(resourceId, descriptor);
        UseBuffer(resourceId, BufferStage.Undefined, ResourceUsage.Write);
        //Write(resourceId);
        return resourceId;
    }

    // public uint Read(uint resourceId)
    // {
    //     {
    //         var dep = new Dependency
    //         {
    //             Type = DependencyType.Read,
    //             Id = resourceId
    //         };
    //
    //         if (PassDependencies.TryGetValue(CurrentPassId, out var dependencies))
    //             dependencies.Add(dep);
    //         else
    //             PassDependencies.Add(CurrentPassId, [dep]);
    //     }
    //     {
    //         var action = new ResourceAction
    //         {
    //             Usage = ResourceUsage.Read,
    //             PassId = CurrentPassId,
    //             Type = Resources[resourceId] is ImageResourceDescriptor ? ResourceType.Image : ResourceType.Buffer,
    //         };
    //
    //         if (ResourceActions.TryGetValue(resourceId, out var passes))
    //             passes.Add(action);
    //         else
    //             ResourceActions.Add(resourceId, [action]);
    //     }
    //     return resourceId;
    // }
    //
    // public uint Write(uint resourceId)
    // {
    //     {
    //         var dep = new Dependency
    //         {
    //             Type = DependencyType.Write,
    //             Id = resourceId
    //         };
    //
    //         if (PassDependencies.TryGetValue(CurrentPassId, out var dependencies))
    //             dependencies.Add(dep);
    //         else
    //             PassDependencies.Add(CurrentPassId, [dep]);
    //     }
    //     {
    //         var action = new ResourceAction
    //         {
    //             Usage = ResourceUsage.Write,
    //             PassId = CurrentPassId,
    //             Type = Resources[resourceId] is ImageResourceDescriptor ? ResourceType.Image : ResourceType.Buffer,
    //         };
    //
    //         if (ResourceActions.TryGetValue(resourceId, out var passes))
    //             passes.Add(action);
    //         else
    //             ResourceActions.Add(resourceId, [action]);
    //     }
    //     return resourceId;
    // }

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
                Type = ResourceType.Image,
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