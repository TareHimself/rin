using rin.Core;
using rin.Core.Extensions;
using rsl;
using TerraFX.Interop.Vulkan;

namespace rin.Graphics.FrameGraph;

public class CompiledGraph : ICompiledGraph
{
    private readonly Dictionary<IResourceHandle, IGraphResource> _resources = [];
    private readonly Dictionary<IResourceHandle, IResourceDescriptor> _descriptors;
    private readonly ICompiledGraphNode[] _nodes;
    private readonly DeviceBuffer? _buffer = null;
    private ulong _bufferOffset = 0;
    public CompiledGraph(Dictionary<IResourceHandle, IResourceDescriptor> descriptors, ICompiledGraphNode[] nodes)
    {
        var memoryNeeded = descriptors.Values.Aggregate((ulong)0, (total, descriptor) =>
        {
            if (descriptor is MemoryResourceDescriptor asMemoryDescriptor)
            {
                return total + asMemoryDescriptor.Size;
            }

            return total;
        });

        if (SRuntime.Get().IsModuleLoaded<SGraphicsModule>() && memoryNeeded > 0)
        {
            _buffer = SGraphicsModule.Get().GetAllocator().NewStorageBuffer(memoryNeeded,debugName: "Compiled Frame Graph Memory");
        }
        
        
        _descriptors = descriptors;
        _nodes = nodes;
    }
    
    public void Dispose()
    {
        foreach (var (_,resource) in _resources)
        {
            resource.Dispose();
        }
    }

    public IGraphResource GetResource(IResourceHandle handle)
    {
        {
            if (_resources.TryGetValue(handle, out var resource)) return resource;
        }

        if (!_descriptors.TryGetValue(handle, out var descriptor)) throw new Exception("Unknown resource handle");

        {
            if (_buffer != null && descriptor is MemoryResourceDescriptor asMemoryDescriptor)
            {
                var view = _buffer.GetView(_bufferOffset, asMemoryDescriptor.Size);
                _bufferOffset += view.Size;
                _resources.Add(handle,view);
                return view;
            }
        }
        
        {
            if (descriptor is ImageResourceDescriptor asImageResourceDescriptor)
            {
                var image = SGraphicsModule.Get().CreateImage(new VkExtent3D()
                {
                    width = asImageResourceDescriptor.Width,
                    height = asImageResourceDescriptor.Height,
                    depth = 1
                },asImageResourceDescriptor.Format,asImageResourceDescriptor.Flags,debugName: "Frame Graph Image");
                _resources.Add(handle,image);
                return image;
            }
        }
        
        throw new Exception("Failed To Allocate Resource");
    }

    public void Run(Frame frame)
    {
        HashSet<IPass> completed = [];

        Queue<ICompiledGraphNode> pending = _nodes.ToQueue();

        var cmd = frame.GetCommandBuffer();

        // OPTIMIZE THIS LATER
        while (pending.NotEmpty())
        {
            var next = pending.Dequeue();
            if (next.Dependencies.IsSubsetOf(completed))
            {
                next.Pass.Execute(this,frame,cmd);
                completed.Add(next.Pass);
            }
            else
            {
                pending.Enqueue(next);
            }
        }
    }
}