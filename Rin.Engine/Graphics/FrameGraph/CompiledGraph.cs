namespace Rin.Engine.Graphics.FrameGraph;

public sealed class CompiledGraph : ICompiledGraph
{
    private readonly Dictionary<uint, IDeviceBuffer> _buffers = [];
    private readonly Dictionary<uint, IResourceDescriptor> _descriptors;
    private readonly Frame _frame;
    private readonly Dictionary<uint, IGraphImage> _images = [];
    private readonly IEnumerable<ExecutionGroup> _nodes;
    private readonly IResourcePool _resourcePool;


    public CompiledGraph(IResourcePool resourcePool, Frame frame, Dictionary<uint, IResourceDescriptor> descriptors,
        IEnumerable<ExecutionGroup> nodes)
    {
        _resourcePool = resourcePool;
        _frame = frame;
        _descriptors = descriptors;

        _nodes = nodes;
    }

    public void Dispose()
    {
        foreach (var image in _images.Values) image.Dispose();

        //if(resource is not DeviceImage) resource.Dispose();
        _images.Clear();
        foreach (var buffers in _buffers.Values) buffers.Dispose();

        _buffers.Clear();
    }


    public IGraphImage GetImage(uint id)
    {
        {
            if (_images.TryGetValue(id, out var resource)) return resource;
        }

        {
            if (_descriptors.TryGetValue(id, out var descriptor))
            {
                {
                    if (descriptor is ImageResourceDescriptor asImageResourceDescriptor)
                    {
                        var image = _resourcePool.CreateImage(asImageResourceDescriptor, _frame);
                        _images.Add(id, image);
                        return image;
                    }
                }

                {
                    if (descriptor is ExternalImageResourceDescriptor asExternalImageResourceDescriptor)
                    {
                        var image = asExternalImageResourceDescriptor.Image;
                        _images.Add(id, image);
                        return image;
                    }
                }
            }
        }

        throw new ResourceAllocationException(id);
    }

    public IDeviceBufferView GetBuffer(uint id)
    {
        {
            if (_buffers.TryGetValue(id, out var resource)) return resource.GetView();
        }
        if (_descriptors.TryGetValue(id, out var descriptor) &&
            descriptor is BufferResourceDescriptor asMemoryDescriptor)
        {
            var buffer = _resourcePool.CreateBuffer(asMemoryDescriptor, _frame);
            _buffers.Add(id, buffer);
            return buffer.GetView();
        }

        throw new ResourceAllocationException(id);
    }


    public void Execute(Frame frame, IRenderData context, TaskPool taskPool)
    {
        var executionContext =
            new VulkanExecutionContext(frame.GetPrimaryCommandBuffer(), frame.GetDescriptorAllocator());
        foreach (var stage in _nodes)
        foreach (var pass in stage.Passes)
            pass.Execute(this, executionContext);
    }
}