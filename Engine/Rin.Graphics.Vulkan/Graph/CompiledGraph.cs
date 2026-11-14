using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Shared.Pooling;
using Rin.Core.Shared.Time;
using Rin.Graphics.Vulkan.Images;

namespace Rin.Graphics.Vulkan.Graph;

/// <summary>
///     A compiled frame graph. Its builder fills it, and gets it back to fill again once it is disposed.
/// </summary>
public sealed class CompiledGraph : ICompiledGraph, IResettable
{
    private readonly Dictionary<uint, IVulkanDeviceBuffer> _buffers = [];
    private readonly Dictionary<uint, IResourceDescriptor> _descriptors = [];
    private readonly Frame _frame;
    private readonly Dictionary<uint, IVulkanImage> _images = [];
    private readonly List<ExecutionGroup> _nodes = [];
    private readonly List<IDisposable> _disposables = [];
    private readonly GraphBuilder _owner;
    private readonly IResourcePool _resourcePool;
    private readonly Stack<BarrierPass> _spareBarriers = [];
    private readonly Stack<ExecutionGroup> _spareGroups = [];
    private bool _disposed;

    internal CompiledGraph(IResourcePool resourcePool, Frame frame, GraphBuilder owner)
    {
        _resourcePool = resourcePool;
        _frame = frame;
        _owner = owner;
    }

    internal Dictionary<uint, IResourceDescriptor> Descriptors => _descriptors;
    internal List<IDisposable> Disposables => _disposables;
    internal IReadOnlyList<ExecutionGroup> Groups => _nodes;

    internal void Reopen()
    {
        _disposed = false;
    }

    internal ExecutionGroup AddGroup()
    {
        var group = _spareGroups.TryPop(out var spare) ? spare : new ExecutionGroup();
        _nodes.Add(group);
        return group;
    }

    /// <summary>
    ///     Adds a group holding one barrier pass and returns the pass to fill.
    /// </summary>
    internal BarrierPass AddBarrier(uint id)
    {
        var barrier = _spareBarriers.TryPop(out var spare) ? spare : new BarrierPass();
        barrier.Id = id;
        AddGroup().Passes.Add(barrier);
        return barrier;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

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

        _owner.Recycle(this);
    }

    public void Reset()
    {
        if (!_disposed) throw new InvalidOperationException("A compiled graph is reset only after it is disposed.");
        _descriptors.Clear();
        foreach (var group in _nodes)
        {
            if (group.IsBarrier)
            {
                var barrier = (BarrierPass)group.Passes[0];
                barrier.Reset();
                _spareBarriers.Push(barrier);
            }

            group.Passes.Clear();
            _spareGroups.Push(group);
        }

        _nodes.Clear();
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
        foreach (var stage in _nodes)
        {
            foreach (var pass in stage.Passes)
            {
                pass.Execute(this, context);
            }
        }
    }
}