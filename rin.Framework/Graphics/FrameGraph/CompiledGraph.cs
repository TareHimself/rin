using rin.Framework.Core;
using rin.Framework.Core.Extensions;
using TerraFX.Interop.Vulkan;

namespace rin.Framework.Graphics.FrameGraph;

public class CompiledGraph : ICompiledGraph
{
    private readonly IDeviceBuffer? _buffer;
    private readonly Dictionary<uint, IDeviceBufferView> _buffers = [];
    private readonly Dictionary<uint, IResourceDescriptor> _descriptors;
    private readonly Frame _frame;
    private readonly Dictionary<uint, IGraphImage> _images = [];
    private readonly ICompiledGraphNode[] _nodes;
    private readonly Dictionary<uint, IPass> _passes;
    private readonly IResourcePool _resourcePool;
    private ulong _bufferOffset;

    public CompiledGraph(IResourcePool resourcePool, Frame frame, Dictionary<uint, IResourceDescriptor> descriptors,
        ICompiledGraphNode[] nodes)
    {
        _resourcePool = resourcePool;
        _frame = frame;
        var memoryNeeded = descriptors.Values.Aggregate((ulong)0, (total, descriptor) =>
        {
            if (descriptor is BufferResourceDescriptor asMemoryDescriptor) return total + asMemoryDescriptor.Size;

            return total;
        });

        if (SRuntime.Get().IsModuleLoaded<SGraphicsModule>() && memoryNeeded > 0)
        {
            var pooledView = resourcePool.CreateBuffer(new BufferResourceDescriptor(memoryNeeded), frame);
            pooledView = new DeviceBufferWriteValidator(pooledView);
            _buffer = pooledView; // SGraphicsModule.Get().NewStorageBuffer(memoryNeeded,debugName: "Compiled Frame Graph Memory");
        }

        _descriptors = descriptors;

        _nodes = nodes;
        _passes = _nodes.ToDictionary(c => c.Pass.Id, c => c.Pass);
    }

    public void Dispose()
    {
        foreach (var (_, resource) in _images) resource.Dispose();
        //if(resource is not DeviceImage) resource.Dispose();
        _images.Clear();
        _buffers.Clear();
        _buffer?.Dispose();
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
            if (_buffers.TryGetValue(id, out var resource)) return resource;
        }

        if (_buffer != null && _descriptors.TryGetValue(id, out var descriptor) &&
            descriptor is BufferResourceDescriptor asMemoryDescriptor)
        {
            var view = _buffer.GetView(_bufferOffset, asMemoryDescriptor.Size);
            _bufferOffset += view.Size;
            _buffers.Add(id, view);
            return view;
        }

        throw new ResourceAllocationException(id);
    }

    public IPass GetPass(uint id)
    {
        return _passes[id];
    }

    public void Execute(Frame frame, IRenderContext context)
    {
        HashSet<IPass> completed = [];

        var pending = _nodes.ToQueue();

        var cmd = frame.GetCommandBuffer();

        cmd.UnBindShader(VkShaderStageFlags.VK_SHADER_STAGE_GEOMETRY_BIT);
        // OPTIMIZE THIS LATER
        while (pending.NotEmpty())
        {
            var next = pending.Dequeue();
            if (next.Dependencies.IsSubsetOf(completed))
            {
                next.Pass.Execute(this, frame, context);
                completed.Add(next.Pass);
            }
            else
            {
                pending.Enqueue(next);
            }
        }
    }
}