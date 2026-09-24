using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Shared.Time;
using Rin.Graphics.Vulkan.Images;

namespace Rin.Graphics.Vulkan.Graph;

public sealed class CompiledGraph : ICompiledGraph
{
    private readonly Dictionary<uint, IVulkanDeviceBuffer> _buffers = [];
    private readonly Dictionary<uint, IResourceDescriptor> _descriptors;
    private readonly Frame _frame;
    private readonly Dictionary<uint, IVulkanImage> _images = [];
    private readonly IEnumerable<ExecutionGroup> _nodes;
    private readonly List<IDisposable> _disposables;
    private readonly IResourcePool _resourcePool;


    public CompiledGraph(IResourcePool resourcePool, Frame frame, Dictionary<uint, IResourceDescriptor> descriptors,
        IEnumerable<ExecutionGroup> nodes, List<IDisposable> disposables)
    {
        _resourcePool = resourcePool;
        _frame = frame;
        _descriptors = descriptors;

        _nodes = nodes;
        _disposables = disposables;
    }

    public void Dispose()
    {
        foreach (var image in _images.Values) (image as IDisposable)?.Dispose();
        foreach (var buffer in _buffers.Values) buffer.Dispose();
        foreach (var (id, descriptor) in _descriptors)
            if (descriptor is IExternalResourceDescriptor external && !_images.ContainsKey(id) &&
                !_buffers.ContainsKey(id))
                external.Resource.Dispose();

        _images.Clear();
        _buffers.Clear();

        // Passes that were pruned are already disposed at compile time (GraphBuilder.Compile); this
        // covers passes that actually ran, once the frame that used them is done with this graph.
        foreach (var stage in _nodes)
        foreach (var pass in stage.Passes)
            (pass as IDisposable)?.Dispose();

        foreach (var disposable in _disposables) disposable.Dispose();
        _disposables.Clear();
    }

    public ResourceHandle GetImage(uint id)
    {
        if (_images.TryGetValue(id, out var resource)) return resource.Handle;

        if (_descriptors.TryGetValue(id, out var descriptor))
        {
            IVulkanImage? created = descriptor switch
            {
                TextureResourceDescriptor asResourceDescriptor => _resourcePool.CreateTexture(asResourceDescriptor,
                    _frame),
                CubemapResourceDescriptor asResourceDescriptor => _resourcePool.CreateCubemap(asResourceDescriptor,
                    _frame),
                TextureArrayResourceDescriptor asResourceDescriptor => _resourcePool.CreateTextureArray(
                    asResourceDescriptor, _frame),
                ExternalVulkanTextureResourceDescriptor asExternalDescriptor => asExternalDescriptor.Resource,
                ExternalVulkanCubemapResourceDescriptor asExternalDescriptor => asExternalDescriptor.Resource,
                ExternalVulkanTextureArrayResourceDescriptor asExternalDescriptor => asExternalDescriptor.Resource,
                _ => null
            };

            if (created is not null)
            {
                _images.Add(id, created);
                return created.Handle;
            }
        }

        throw new ResourceAllocationException(id);
    }

    public DeviceBufferView GetBuffer(uint id)
    {
        {
            if (_buffers.TryGetValue(id, out var resource)) return resource.GetView();
        }
        if (_descriptors.TryGetValue(id, out var descriptor))
        {
            var buffer = descriptor switch
            {
                BufferResourceDescriptor asMemoryDescriptor => _resourcePool.CreateBuffer(asMemoryDescriptor, _frame),
                ExternalVulkanBufferResourceDescriptor asExternalDescriptor => asExternalDescriptor.Resource,
                _ => null
            };

            if (buffer is not null)
            {
                _buffers.Add(id, buffer);
                return buffer.GetView();
            }
        }

        throw new ResourceAllocationException(id);
    }


    public void OnSubmitted()
    {
        foreach (var stage in _nodes)
        foreach (var pass in stage.Passes)
            (pass as IUploadPass)?.OnSubmitted();
    }

    public void Execute(IExecutionContext context)
    {
        // foreach (var stage in _nodes)
        // foreach (var pass in stage.Passes)
        //     pass.Execute(this, context);
        foreach (var stage in _nodes)
        {
            foreach (var pass in stage.Passes)
            {
                pass.Execute(this, context);
            }
        }
    }
}