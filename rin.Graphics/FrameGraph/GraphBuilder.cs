using System.Collections.Frozen;
using rin.Core.Extensions;
using rsl;
using TerraFX.Interop.Vulkan;

namespace rin.Graphics.FrameGraph;

public class GraphBuilder : IGraphBuilder
{
    private readonly Dictionary<IResourceHandle, ImageResourceDescriptor> _images = [];
    private readonly Dictionary<IResourceHandle, MemoryResourceDescriptor> _memory = [];
    private readonly Dictionary<string, IResourceHandle> _resourceNamesToHandles = [];
    private readonly Dictionary<IResourceHandle,IResourceDescriptor> _resources = [];
    private readonly Dictionary<string, IPass> _passes = [];
    private readonly Dictionary<IPass, HashSet<IResourceHandle>> _passWrites = [];
    private readonly Dictionary<IPass, HashSet<IResourceHandle>> _passReads = [];
    private readonly Dictionary<IResourceHandle, HashSet<IPass>> _resourceWrites = [];
    private readonly Dictionary<IResourceHandle, HashSet<IPass>> _resourceReads = [];
    
    public IGraphBuilder AddPass(IPass pass)
    {
        _passes.Add(pass.Name,pass);
        return this;
    }
    
    public IResourceHandle MakeResourceHandle(IResourceDescriptor descriptor, string name)
    {
        return new ResourceHandle(name);
    }

    public IResourceHandle RequestImage(IPass pass,uint width, uint height, ImageFormat format, VkImageLayout initialLayout, string? name = null)
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
        var imageName = name ?? $"image-{Guid.NewGuid().ToString()}";
        var handle = MakeResourceHandle(descriptor, imageName);
        _images.Add(handle, descriptor);
        _resources.Add(handle,descriptor);
        _resourceNamesToHandles.Add(imageName,handle);
        Write(pass,handle);
        return handle;
    }

    public IResourceHandle RequestMemory(IPass pass,ulong size,string? name = null)
    {
        var descriptor = new MemoryResourceDescriptor()
        {
            Size = size
        };
        var memoryName = name ?? $"memory-{size}-{Guid.NewGuid().ToString()}";
        var handle = MakeResourceHandle(descriptor,memoryName);
        _memory.Add(handle, descriptor);
        _resources.Add(handle,descriptor);
        _resourceNamesToHandles.Add(memoryName,handle);
        Write(pass,handle);
        return handle;
    }

    public IResourceHandle Read(IPass pass,string name)
    {
        if (_resourceNamesToHandles.TryGetValue(name,out var handle))
        {
            return Read(pass,handle);
        }
        
        throw new Exception("Resource does not exist");
    }

    public IResourceHandle Write(IPass pass,string name)
    {
        if (_resourceNamesToHandles.TryGetValue(name,out var handle))
        {
            return Write(pass,handle);
        }
        
        throw new Exception("Resource does not exist");
    }

    public IResourceHandle Read(IPass pass,IResourceHandle handle)
    {
        if (_resources.TryGetValue(handle, out var resource))
        {
            {
                if (_passReads.TryGetValue(pass, out var read))
                {
                    read.Add(handle);
                }
                else
                {
                    _passReads.Add(pass,[handle]);
                }
            }
            
            {
                if (_resourceReads.TryGetValue(handle, out var read))
                {
                    read.Add(pass);
                }
                else
                {
                    _resourceReads.Add(handle,[pass]);
                }
            }
        }
        else
        {
            throw new Exception("Resource does not exist");
        }
        return handle;
    }

    public IResourceHandle Write(IPass pass,IResourceHandle handle)
    {
        if (_resources.TryGetValue(handle, out var resource))
        {
            {
                if (_passWrites.TryGetValue(pass, out var write))
                {
                    write.Add(handle);

                }
                else
                {
                    _passWrites.Add(pass, [handle]);
                }
            }
            {
                if (_resourceWrites.TryGetValue(handle, out var write))
                {
                    write.Add(pass);
                }
                else
                {
                    _resourceWrites.Add(handle,[pass]);
                }
            }
        }
        else
        {
            throw new Exception("Resource does not exist");
        }
        return handle;
    }

    protected void Configure()
    {
        _passWrites.Clear();
        _passReads.Clear();
        foreach (var (_,pass) in _passes)
        {
            pass.Configure(this);
        }
    }

    struct ResourceCounter
    {
        public required IResourceHandle Handle;
        public required int Reads;
        public required int Writes;
    }
    
    public ICompiledGraph? Compile()
    {
        Configure();

        var top = _passes.Values.Where(p => !_passReads.ContainsKey(p)).ToArray();
        var terminals = _passes.Values.Where(p => p.IsTerminal).ToArray();
        
        if (terminals.Empty())
        {
            return null;
        }

        HashSet<IPass> validPasses = [];
        Dictionary<IPass,ICompiledGraphNode> nodes = [];
        HashSet<IResourceHandle> validResources = [];
        Queue<IPass> toSearch = terminals.Aggregate(new Queue<IPass>(),(result,terminal) =>
        {
            nodes.Add(terminal, new CompiledGraphNode()
            {
                Pass = terminal,
                Dependencies = [],
                Dependents = []
            });
            {
                if (_passWrites.TryGetValue(terminal, out var writes))
                {
                    validResources.UnionWith(writes);
                }
            }
            result.Enqueue(terminal);
            return result;
        });
        
        while (toSearch.NotEmpty())
        {
            var current = toSearch.Dequeue();
            
            validPasses.Add(current);
            var reads = (_passReads.TryGetValue(current, out var read) ? read : []);
            validResources.UnionWith(reads);
            foreach(var pass in reads.SelectMany(c => _resourceWrites[c]).Where(c => !toSearch.Contains(c)))
            {
                {
                    if (!nodes.TryGetValue(pass, out var node))
                    {
                        nodes.Add(pass,new CompiledGraphNode()
                        {
                            Pass = pass,
                            Dependencies = [],
                            Dependents = [current]
                        });
                    }
                    else
                    {
                        node.Dependents.Add(pass);
                    }
                }
                nodes[current].Dependencies.Add(pass);
                toSearch.Enqueue(pass);
            }
        }

        var descriptors = new Dictionary<IResourceHandle, IResourceDescriptor>();
        foreach (var resourceHandle in validResources)
        {
            descriptors.Add(resourceHandle,_resources[resourceHandle]);
        }
        return new CompiledGraph(descriptors, nodes.Values.ToArray());
    }

    public void Reset()
    {
        _images.Clear();
        _memory.Clear();
        _resourceNamesToHandles.Clear();
        _resources.Clear();
        _passes.Clear();
        _passWrites.Clear();
        _passReads.Clear();
        _resourceWrites.Clear();
        _resourceReads.Clear();
    }
}