using Rin.Framework.Graphics.Graph;
using Rin.Framework.Graphics.Images;

namespace Rin.Framework.Graphics.Vulkan.Graph;

public sealed class CompiledGraph : ICompiledGraph
{
    private readonly Dictionary<uint, IDeviceBuffer> _buffers = [];
    private readonly Dictionary<uint, IResourceDescriptor> _descriptors;
    private readonly Frame _frame;
    private readonly Dictionary<uint, IDisposableTexture> _textures = [];
    private readonly Dictionary<uint, IDisposableTextureArray> _textureArrays = [];
    private readonly Dictionary<uint, IDisposableCubemap> _cubemaps = [];
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
        foreach (var image in _textures.Values) image.Dispose();
        foreach (var image in _textureArrays.Values) image.Dispose();
        foreach (var image in _cubemaps.Values) image.Dispose();
        foreach (var buffers in _buffers.Values) buffers.Dispose();

        _textures.Clear();
        _textureArrays.Clear();
        _cubemaps.Clear();
        _buffers.Clear();
    }
    
    public ITexture GetTexture(uint id)
    {
        {
            if (_textures.TryGetValue(id, out var resource)) return resource;
        }

        {
            if (_descriptors.TryGetValue(id, out var descriptor))
            {
                {
                    if (descriptor is TextureResourceDescriptor asResourceDescriptor)
                    {
                        var resource = _resourcePool.CreateTexture(asResourceDescriptor, _frame);
                        _textures.Add(id, resource);
                        return resource;
                    }
                }

                {
                    if (descriptor is ExternalVulkanTextureResourceDescriptor asExternalDescriptor)
                    {
                        var resource = asExternalDescriptor.Resource;
                        _textures.Add(id, resource);
                        return resource;
                    }
                }
            }
        }

        throw new ResourceAllocationException(id);
    }

    public ITextureArray GetTextureArray(uint id)
    {
        {
            if (_textureArrays.TryGetValue(id, out var resource)) return resource;
        }

        {
            if (_descriptors.TryGetValue(id, out var descriptor))
            {
                {
                    if (descriptor is TextureArrayResourceDescriptor asResourceDescriptor)
                    {
                        var resource = _resourcePool.CreateTextureArray(asResourceDescriptor, _frame);
                        _textureArrays.Add(id, resource);
                        return resource;
                    }
                }

                {
                    if (descriptor is ExternalVulkanTextureArrayResourceDescriptor asExternalDescriptor)
                    {
                        var resource = asExternalDescriptor.Resource;
                        _textureArrays.Add(id, resource);
                        return resource;
                    }
                }
            }
        }

        throw new ResourceAllocationException(id);
    }

    public ICubemap GetCubemap(uint id)
    {
        {
            if (_cubemaps.TryGetValue(id, out var resource)) return resource;
        }

        {
            if (_descriptors.TryGetValue(id, out var descriptor))
            {
                {
                    if (descriptor is CubemapResourceDescriptor asResourceDescriptor)
                    {
                        var resource = _resourcePool.CreateCubemap(asResourceDescriptor, _frame);
                        _cubemaps.Add(id, resource);
                        return resource;
                    }
                }

                {
                    if (descriptor is ExternalVulkanCubemapResourceDescriptor asExternalDescriptor)
                    {
                        var resource = asExternalDescriptor.Resource;
                        _cubemaps.Add(id, resource);
                        return resource;
                    }
                }
            }
        }

        throw new ResourceAllocationException(id);
    }

    public DeviceBufferView GetBuffer(uint id)
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


    public void Execute(IExecutionContext context)
    {
        foreach (var stage in _nodes)
        foreach (var pass in stage.Passes)
            pass.Execute(this, context);
    }
}