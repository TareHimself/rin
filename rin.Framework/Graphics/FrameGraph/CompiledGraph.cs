using rin.Framework.Core;
using rin.Framework.Core.Extensions;
using TerraFX.Interop.Vulkan;

namespace rin.Framework.Graphics.FrameGraph;

public class CompiledGraph : ICompiledGraph
{
    private readonly IImagePool _imagePool;
    private readonly Dictionary<uint, IGraphResource> _resources = [];
    private readonly Dictionary<uint, IResourceDescriptor> _descriptors;
    private readonly ICompiledGraphNode[] _nodes;
    private readonly IDeviceBuffer? _buffer = null;
    private ulong _bufferOffset = 0;
    private Frame _frame;
    public CompiledGraph(IImagePool imagePool,Frame frame,Dictionary<uint, IResourceDescriptor> descriptors, ICompiledGraphNode[] nodes)
    {
        _imagePool = imagePool;
        _frame = frame;
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
            _buffer = SGraphicsModule.Get().NewStorageBuffer(memoryNeeded,debugName: "Compiled Frame Graph Memory");
        }
        
        _descriptors = descriptors;
        
        _nodes = nodes;
    }
    
    public void Dispose()
    {
        _buffer?.Dispose();
        foreach (var (_,resource) in _resources)
        {
            if(resource is not DeviceImage) resource.Dispose();
        }
    }

    public IGraphResource GetResource(uint handle)
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
                var image = _imagePool.CreateImage(asImageResourceDescriptor,_frame);
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