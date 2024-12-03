using System.Collections.Frozen;
using rin.Framework.Core.Extensions;
using rsl;
using TerraFX.Interop.Vulkan;

namespace rin.Framework.Graphics.FrameGraph;

public class GraphBuilder : IGraphBuilder
{
    private readonly Dictionary<string, ImageResourceDescriptor> _images = [];
    private readonly Dictionary<string, MemoryResourceDescriptor> _memory = [];
    private readonly Dictionary<string,IResourceDescriptor> _resources = [];
    private readonly Dictionary<string, IPass> _passes = [];
    private readonly Dictionary<IPass, HashSet<string>> _passWrites = [];
    private readonly Dictionary<IPass, HashSet<string>> _passReads = [];
    private readonly Dictionary<string, HashSet<IPass>> _resourceWrites = [];
    private readonly Dictionary<string, HashSet<IPass>> _resourceReads = [];
    
    public IGraphBuilder AddPass(IPass pass)
    {
        _passes.Add(pass.Name,pass);
        return this;
    }
    

    public string RequestImage(IPass pass,uint width, uint height, ImageFormat format, VkImageLayout initialLayout, string? name = null)
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
        var resourceId = name ?? $"image-{Guid.NewGuid().ToString()}";
        _images.Add(resourceId, descriptor);
        _resources.Add(resourceId,descriptor);
        Write(pass,resourceId);
        return resourceId;
    }

    public string RequestMemory(IPass pass,ulong size,string? name = null)
    {
        var descriptor = new MemoryResourceDescriptor()
        {
            Size = size
        };
        var resourceId = name ?? $"memory-{size}-{Guid.NewGuid().ToString()}";
        _memory.Add(resourceId, descriptor);
        _resources.Add(resourceId,descriptor);
        Write(pass,resourceId);
        return resourceId;
    }
    

    public string Read(IPass pass,string id)
    {
        if (_resources.TryGetValue(id, out var resource))
        {
            {
                if (_passReads.TryGetValue(pass, out var read))
                {
                    read.Add(id);
                }
                else
                {
                    _passReads.Add(pass,[id]);
                }
            }
            
            {
                if (_resourceReads.TryGetValue(id, out var read))
                {
                    read.Add(pass);
                }
                else
                {
                    _resourceReads.Add(id,[pass]);
                }
            }
        }
        else
        {
            throw new Exception("Resource does not exist");
        }
        return id;
    }

    public string Write(IPass pass,string id)
    {
        if (_resources.TryGetValue(id, out var resource))
        {
            {
                if (_passWrites.TryGetValue(pass, out var write))
                {
                    write.Add(id);

                }
                else
                {
                    _passWrites.Add(pass, [id]);
                }
            }
            {
                if (_resourceWrites.TryGetValue(id, out var write))
                {
                    write.Add(pass);
                }
                else
                {
                    _resourceWrites.Add(id,[pass]);
                }
            }
        }
        else
        {
            throw new Exception("Resource does not exist");
        }
        return id;
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
    
    public ICompiledGraph? Compile(IImagePool imagePool)
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
        HashSet<string> validResources = [];
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

        var descriptors = new Dictionary<string, IResourceDescriptor>();
        foreach (var resourceHandle in validResources)
        {
            descriptors.Add(resourceHandle,_resources[resourceHandle]);
        }
        return new CompiledGraph(imagePool,descriptors, nodes.Values.ToArray());
    }

    public void Reset()
    {
        _images.Clear();
        _memory.Clear();
        _resources.Clear();
        _passes.Clear();
        _passWrites.Clear();
        _passReads.Clear();
        _resourceWrites.Clear();
        _resourceReads.Clear();
    }
}