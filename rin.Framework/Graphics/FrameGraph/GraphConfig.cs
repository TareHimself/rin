using System.Collections.Frozen;
using TerraFX.Interop.Vulkan;

namespace rin.Framework.Graphics.FrameGraph;

public class GraphConfig(GraphBuilder builder) : IGraphConfig
{
    public enum DependencyType
    {
        Pass,
        Read,
        Write
    }
    
    public class Dependency
    {
        public DependencyType Type { get; set; }
        public uint Id { get; set; }
    }

    public enum ActionType
    {
        Read,
        Write
    }
    public class ResourceAction
    {
        public ActionType Type { get; set; }
        public uint PassId { get; set; }
    }
    
    public uint CurrentPassId { get; set; }
    
    public readonly Dictionary<uint,List<Dependency>> PassDependencies = [];
    public readonly Dictionary<uint,IResourceDescriptor> Resources = [];
    public readonly Dictionary<uint,List<ResourceAction>> ResourceActions = [];
    
    
    public uint CreateImage(uint width, uint height, ImageFormat format, ImageLayout initialLayout)
    {
        var flags = format is ImageFormat.Depth or ImageFormat.Stencil
            ? VkImageUsageFlags.VK_IMAGE_USAGE_DEPTH_STENCIL_ATTACHMENT_BIT
            : VkImageUsageFlags.VK_IMAGE_USAGE_STORAGE_BIT | VkImageUsageFlags.VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT;
        
        flags |= VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_SRC_BIT | VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_DST_BIT | VkImageUsageFlags.VK_IMAGE_USAGE_SAMPLED_BIT;
        var descriptor = new ImageResourceDescriptor()
        {
            Width = width,
            Height = height,
            Format = format,
            Flags = flags,
            InitialLayout = initialLayout
        };
        var resourceId = builder.MakeId();
        Resources.Add(resourceId,descriptor);
        Write(resourceId);
        return resourceId;
    }

    public uint AllocateBuffer(int size)
    {
        var descriptor = new MemoryResourceDescriptor()
        {
            Size = size
        };
        var resourceId = builder.MakeId();
        // _memory.Add(resourceId, descriptor);
        Resources.Add(resourceId,descriptor);
        Write(resourceId);
        return resourceId;
    }

    public uint Read(uint resourceId)
    {
        {
            var dep = new Dependency()
            {
                Type = DependencyType.Read,
                Id = resourceId
            };
            
            if (PassDependencies.TryGetValue(CurrentPassId, out var dependencies))
            {
                dependencies.Add(dep);
            }
            else
            {
                PassDependencies.Add(CurrentPassId,[dep]);
            }
        }
        {
            var action = new ResourceAction()
            {
                Type = ActionType.Read,
                PassId = CurrentPassId
            };
            
            if (ResourceActions.TryGetValue(resourceId, out var passes))
            {
                passes.Add(action);
            }
            else
            {
                ResourceActions.Add(resourceId,[action]);
            }
        }
        return resourceId;
    }

    public uint Write(uint resourceId)
    {
        {
            var dep = new Dependency()
            {
                Type = DependencyType.Write,
                Id = resourceId
            };
            
            if (PassDependencies.TryGetValue(CurrentPassId, out var dependencies))
            {
                dependencies.Add(dep);
            }
            else
            {
                PassDependencies.Add(CurrentPassId,[dep]);
            }
        }
        {
            var action = new ResourceAction()
            {
                Type = ActionType.Write,
                PassId = CurrentPassId
            };
            
            if (ResourceActions.TryGetValue(resourceId, out var passes))
            {
                passes.Add(action);
            }
            else
            {
                ResourceActions.Add(resourceId,[action]);
            }
        }
        return resourceId;
    }

    public uint DependOn(uint passId)
    {
        {
            var dep = new Dependency()
            {
                Type = DependencyType.Pass,
                Id = passId
            };
            
            if (PassDependencies.TryGetValue(CurrentPassId, out var dependencies))
            {
                dependencies.Add(dep);
            }
            else
            {
                PassDependencies.Add(CurrentPassId,[dep]);
            }
        }
        return passId;
    }
}