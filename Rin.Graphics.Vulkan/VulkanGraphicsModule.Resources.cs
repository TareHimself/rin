using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Shaders;
using Rin.Core.Shared;
using Rin.Core.Shared.Buffers;
using Rin.Graphics.Vulkan.Descriptors;
using Rin.Graphics.Vulkan.Images;
using TerraFX.Interop.Vulkan;

namespace Rin.Graphics.Vulkan;

/// <summary>
///     Registers every image/buffer resource (bindless-sampled or not) behind a <see cref="ResourceHandle" /> so
///     callers never need to hold a concrete backend object. Formerly a separate VulkanBindlessImageFactory class;
///     folded in directly since VulkanGraphicsModule already forwarded nearly every member 1:1.
/// </summary>
public partial class VulkanGraphicsModule
{
    private const uint MaxTextures = 2048;
    private const uint MaxCubemaps = 512;
    private const uint MaxTextureArrays = 512;
    private const uint SamplerCount = 6;
    private const uint SamplersBinding = 0;
    private const uint TexturesBinding = 1;
    private const uint TextureArraysBinding = 2;
    private const uint CubemapsBinding = 3;

    private const uint ResourceDescriptorTotal = MaxTextures + MaxCubemaps + MaxTextureArrays + SamplerCount;

    private readonly Lock _resourceSync = new();

    private readonly IdFactory<uint> _textureIdFactory = new();
    private readonly IdFactory<uint> _textureArrayIdFactory = new();
    private readonly IdFactory<uint> _cubemapIdFactory = new();
    private readonly IdFactory<uint> _bufferIdFactory = new();

    private readonly List<BindlessTexture> _textures = [];
    private readonly List<BindlessTextureArray> _textureArrays = [];
    private readonly List<BindlessCubemap> _cubemaps = [];
    private readonly List<IVulkanDeviceBuffer?> _buffers = [];

    private readonly Dictionary<ResourceHandle, TaskCompletionSource<ResourceHandle>> _pendingResourceTasks = [];

    private DescriptorAllocator? _resourceDescriptorAllocator;
    private DescriptorSet _resourceDescriptorSet;
    private VkDescriptorSetLayout _resourceDescriptorSetLayout;
    private VkPipelineLayout _resourcePipelineLayout;

    private IDisposableVulkanTexture _defaultTexture = null!;
    private IDisposableVulkanTextureArray _defaultTextureArray = null!;
    private IDisposableVulkanCubemap _defaultCubemap = null!;

    private void InitBindlessResources()
    {
        const DescriptorBindingFlags flags = DescriptorBindingFlags.PartiallyBound |
                                             DescriptorBindingFlags.UpdateAfterBind;

        _resourceDescriptorAllocator = new DescriptorAllocator(ResourceDescriptorTotal, [
            new PoolSizeRatio(DescriptorType.SampledImage,
                (float)(MaxTextures + MaxCubemaps + MaxTextureArrays) / ResourceDescriptorTotal),
            new PoolSizeRatio(DescriptorType.Sampler, (float)SamplerCount / ResourceDescriptorTotal)
        ], VkDescriptorPoolCreateFlags.VK_DESCRIPTOR_POOL_CREATE_UPDATE_AFTER_BIND_BIT);

        _resourceDescriptorSetLayout = new DescriptorLayoutBuilder()
            .AddBinding(
                SamplersBinding,
                DescriptorType.Sampler,
                ShaderStage.All,
                SamplerCount,
                flags
            )
            .AddBinding(
                TexturesBinding,
                DescriptorType.SampledImage,
                ShaderStage.All,
                MaxTextures,
                flags
            )
            .AddBinding(
                TextureArraysBinding,
                DescriptorType.SampledImage,
                ShaderStage.All,
                MaxTextureArrays,
                flags
            )
            .AddBinding(
                CubemapsBinding,
                DescriptorType.SampledImage,
                ShaderStage.All,
                MaxCubemaps,
                flags
            )
            .Build();

        _resourceDescriptorSet = _resourceDescriptorAllocator.Allocate(_resourceDescriptorSetLayout);

        for (var filter = 0; filter < 2; filter++)
        for (var tiling = 0; tiling < 3; tiling++)
            _resourceDescriptorSet.WriteSampler(SamplersBinding, new SamplerSpec
            {
                Filter = (ImageFilter)filter,
                Tiling = (ImageTiling)tiling
            }, (uint)(filter * 3 + tiling));

        _resourceDescriptorSet.Update();
        _resourcePipelineLayout = _device.CreatePipelineLayout([_resourceDescriptorSetLayout]);

        _textureIdFactory.NewId(); // 0 is invalid id
        _textureArrayIdFactory.NewId();
        _cubemapIdFactory.NewId();
        _bufferIdFactory.NewId();
        _textures.Add(new BindlessTexture());
        _textureArrays.Add(new BindlessTextureArray());
        _cubemaps.Add(new BindlessCubemap());
        _buffers.Add(null);

        var extent = new Extent2D(1, 1);
        var format = ImageFormat.RGBA8;
        _defaultTexture = CreateVulkanTexture(extent, format, usage: ImageUsage.Sampled);
        _defaultTextureArray = CreateVulkanTextureArray(extent, format, 1, usage: ImageUsage.Sampled);
        _defaultCubemap = CreateVulkanCubemap(extent, format, usage: ImageUsage.Sampled);
    }

    private void DisposeBindlessResources()
    {
        if (_resourceDescriptorAllocator is null) return;

        _resourceDescriptorAllocator.Dispose();
        foreach (var resource in _textures) resource.Source?.Dispose();
        foreach (var resource in _textureArrays) resource.Source?.Dispose();
        foreach (var resource in _cubemaps) resource.Source?.Dispose();
        _defaultTexture.Dispose();
        _defaultCubemap.Dispose();
        _defaultTextureArray.Dispose();
        _device.DestroyPipelineLayout(_resourcePipelineLayout);
    }

    public ResourceHandle CreateTexture(in Extent2D size, ImageFormat format, bool mips = false,
        ImageUsage usage = ImageUsage.None)
    {
        var image = CreateVulkanTexture(size, format, mips, usage);
        var isBindless = usage.HasFlag(ImageUsage.Sampled);

        lock (_resourceSync)
        {
            var id = _textureIdFactory.NewId(out var addToArray);
            var handle = new ResourceHandle(ResourceType.Texture, id, isBindless);
            var resource = new BindlessTexture
            {
                Handle = handle,
                Source = image,
                State = BindlessResourceState.PendingBind
            };

            if (addToArray)
                _textures.Add(resource);
            else
                _textures[(int)id] = resource;

            UpdateHandles(resource.Handle);

            return resource.Handle;
        }
    }

    public ResourceHandle CreateTextureArray(in Extent2D size, ImageFormat format, uint count, bool mips = false,
        ImageUsage usage = ImageUsage.None)
    {
        var image = CreateVulkanTextureArray(size, format, count, mips, usage);
        var isBindless = usage.HasFlag(ImageUsage.Sampled);

        lock (_resourceSync)
        {
            var id = _textureArrayIdFactory.NewId(out var addToArray);
            var handle = new ResourceHandle(ResourceType.TextureArray, id, isBindless);
            var resource = new BindlessTextureArray
            {
                Handle = handle,
                Source = image,
                State = BindlessResourceState.PendingBind
            };

            if (addToArray)
                _textureArrays.Add(resource);
            else
                _textureArrays[(int)id] = resource;

            UpdateHandles(resource.Handle);

            return resource.Handle;
        }
    }

    public ResourceHandle CreateCubemap(in Extent2D size, ImageFormat format, bool mips = false,
        ImageUsage usage = ImageUsage.None)
    {
        var image = CreateVulkanCubemap(size, format, mips, usage);
        var isBindless = usage.HasFlag(ImageUsage.Sampled);

        lock (_resourceSync)
        {
            var id = _cubemapIdFactory.NewId(out var addToArray);
            var handle = new ResourceHandle(ResourceType.Cubemap, id, isBindless);
            var resource = new BindlessCubemap
            {
                Handle = handle,
                Source = image,
                State = BindlessResourceState.PendingBind
            };

            if (addToArray)
                _cubemaps.Add(resource);
            else
                _cubemaps[(int)id] = resource;

            UpdateHandles(resource.Handle);

            return resource.Handle;
        }
    }

    public Task<ResourceHandle> CreateTexture(out ResourceHandle handle, ReadOnlySpan<byte> data, in Extent2D size,
        ImageFormat format, bool mips = false, ImageUsage usage = ImageUsage.None)
    {
        // Ownership of `rented` transfers to AsyncCreateTexture, which returns it to the pool once the
        // data has actually been consumed (background upload runs asynchronously after this call returns).
        var rented = ArrayPool<byte>.Shared.Rent(data.Length);
        data.CopyTo(rented);
        var state = (rented, length: data.Length, size, format, mips, usage, method: (Func<BindlessTexture,
            TaskCompletionSource<ResourceHandle>,
            byte[],
            int,
            Extent2D,
            ImageFormat,
            bool,
            ImageUsage, Task>)AsyncCreateTexture);
        return HandleAsyncBindless(out handle, _textures, state,
            static (resource, source, s) =>
                s.method(resource, source, s.rented, s.length, s.size, s.format, s.mips, s.usage));
    }

    public Task<ResourceHandle> CreateTextureArray(out ResourceHandle handle, ReadOnlySpan<byte> data,
        in Extent2D size,
        ImageFormat format, uint count, bool mips = false, ImageUsage usage = ImageUsage.None)
    {
        var rented = ArrayPool<byte>.Shared.Rent(data.Length);
        data.CopyTo(rented);
        var state = (rented, length: data.Length, size, format, count, mips, usage, method: (Func<BindlessTextureArray,
            TaskCompletionSource<ResourceHandle>,
            byte[],
            int,
            Extent2D,
            ImageFormat,
            uint,
            bool,
            ImageUsage, Task>)AsyncCreateTextureArray);
        return HandleAsyncBindless(out handle, _textureArrays, state,
            static (resource, source, s) =>
                s.method(resource, source, s.rented, s.length, s.size, s.format, s.count, s.mips, s.usage));
    }

    public Task<ResourceHandle> CreateCubemap(out ResourceHandle handle, ReadOnlySpan<byte> data, in Extent2D size,
        ImageFormat format, bool mips = false, ImageUsage usage = ImageUsage.None)
    {
        var rented = ArrayPool<byte>.Shared.Rent(data.Length);
        data.CopyTo(rented);
        var state = (rented, length: data.Length, size, format, mips, usage, method: (Func<BindlessCubemap,
            TaskCompletionSource<ResourceHandle>,
            byte[],
            int,
            Extent2D,
            ImageFormat,
            bool,
            ImageUsage, Task>)AsyncCreateCubemap);
        return HandleAsyncBindless(out handle, _cubemaps, state,
            static (resource, source, s) =>
                s.method(resource, source, s.rented, s.length, s.size, s.format, s.mips, s.usage));
    }

    /// <summary>
    ///     Registers an already-constructed image (e.g. a swapchain image owned by the presentation engine) so it
    ///     can be referenced by <see cref="ResourceHandle" /> without going through image creation. The caller is
    ///     responsible for calling <see cref="FreeResourceHandles" /> once the resource is no longer needed.
    /// </summary>
    public ResourceHandle RegisterExternalTexture(IDisposableVulkanTexture image, bool isBindless = false)
    {
        lock (_resourceSync)
        {
            var id = _textureIdFactory.NewId(out var addToArray);
            var handle = new ResourceHandle(ResourceType.Texture, id, isBindless);
            var resource = new BindlessTexture
            {
                Handle = handle,
                Source = image,
                State = BindlessResourceState.Ready
            };

            if (addToArray)
                _textures.Add(resource);
            else
                _textures[(int)id] = resource;

            if (isBindless) UpdateHandles(handle);

            return handle;
        }
    }

    public IDisposableVulkanTexture? GetTexture(in ResourceHandle handle)
    {
        if (handle.Type != ResourceType.Texture || handle.Id == 0 || handle.Id >= _textures.Count) return null;
        lock (_resourceSync)
        {
            var resource = _textures[(int)handle.Id];
            return resource.State != BindlessResourceState.Ready ? null : resource;
        }
    }

    public IDisposableVulkanTextureArray? GetTextureArray(in ResourceHandle handle)
    {
        if (handle.Type != ResourceType.TextureArray || handle.Id == 0 || handle.Id >= _textureArrays.Count)
            return null;
        lock (_resourceSync)
        {
            var resource = _textureArrays[(int)handle.Id];
            return resource.State != BindlessResourceState.Ready ? null : resource;
        }
    }

    public IDisposableVulkanCubemap? GetCubemap(in ResourceHandle handle)
    {
        if (handle.Type != ResourceType.Cubemap || handle.Id == 0 || handle.Id >= _cubemaps.Count) return null;
        lock (_resourceSync)
        {
            var resource = _cubemaps[(int)handle.Id];
            return resource.State != BindlessResourceState.Ready ? null : resource;
        }
    }

    /// <summary>
    ///     Resolves any image handle (texture/cubemap/texture array) to its concrete backend object, for barrier
    ///     and render-attachment code that only needs the common <see cref="IVulkanImage" /> shape.
    /// </summary>
    public IVulkanImage? GetImage(in ResourceHandle handle)
    {
        return handle.Type switch
        {
            ResourceType.Texture => GetTexture(handle),
            ResourceType.Cubemap => GetCubemap(handle),
            ResourceType.TextureArray => GetTextureArray(handle),
            _ => null
        };
    }

    public Extent2D GetExtent(in ResourceHandle handle)
    {
        return GetImage(handle)?.Extent ?? throw new ArgumentException("Invalid or unresolvable resource handle",
            nameof(handle));
    }

    public ImageFormat GetFormat(in ResourceHandle handle)
    {
        return GetImage(handle)?.Format ?? throw new ArgumentException("Invalid or unresolvable resource handle",
            nameof(handle));
    }

    public bool IsValidResourceHandle(in ResourceHandle handle)
    {
        return handle.Id > 0 && handle.Type switch
        {
            ResourceType.Texture => GetTexture(handle) is not null,
            ResourceType.Cubemap => GetCubemap(handle) is not null,
            ResourceType.TextureArray => GetTextureArray(handle) is not null,
            ResourceType.Buffer => ResolveBuffer(handle) is not null,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public void FreeResourceHandles(params ReadOnlySpan<ResourceHandle> handles)
    {
        lock (_resourceSync)
        {
            List<Action> disposes = [];
            var touchedDescriptors = false;
            foreach (var handle in handles)
            {
                if (handle.Id == 0) continue;

                switch (handle.Type)
                {
                    case ResourceType.Texture:
                    {
                        if (handle.IsBindless)
                        {
                            _resourceDescriptorSet.WriteSampledImage(TexturesBinding, _defaultTexture,
                                ImageLayout.ShaderReadOnly, handle.Id);
                            touchedDescriptors = true;
                        }

                        var resource = _textures[(int)handle.Id];
                        if (resource.State == BindlessResourceState.Uploading)
                        {
                            _pendingResourceTasks[handle].SetCanceled();
                            _pendingResourceTasks.Remove(handle);
                        }
                        else
                        {
                            Debug.Assert(resource.Source is not null);
                            disposes.Add(resource.Source.Dispose);
                        }

                        _textures[(int)handle.Id] = new BindlessTexture();
                        _textureIdFactory.FreeId(handle.Id);
                    }
                        break;
                    case ResourceType.Cubemap:
                    {
                        if (handle.IsBindless)
                        {
                            _resourceDescriptorSet.WriteSampledCubemap(TexturesBinding, _defaultCubemap,
                                ImageLayout.ShaderReadOnly, handle.Id);
                            touchedDescriptors = true;
                        }

                        var resource = _cubemaps[(int)handle.Id];
                        if (resource.State == BindlessResourceState.Uploading)
                        {
                            _pendingResourceTasks[handle].SetCanceled();
                            _pendingResourceTasks.Remove(handle);
                        }
                        else
                        {
                            Debug.Assert(resource.Source is not null);
                            disposes.Add(resource.Source.Dispose);
                        }

                        _cubemaps[(int)handle.Id] = new BindlessCubemap();
                        _cubemapIdFactory.FreeId(handle.Id);
                    }
                        break;
                    case ResourceType.TextureArray:
                    {
                        if (handle.IsBindless)
                        {
                            _resourceDescriptorSet.WriteSampledImageArray(TexturesBinding, _defaultTextureArray,
                                ImageLayout.ShaderReadOnly, handle.Id);
                            touchedDescriptors = true;
                        }

                        var resource = _textureArrays[(int)handle.Id];
                        if (resource.State == BindlessResourceState.Uploading)
                        {
                            _pendingResourceTasks[handle].SetCanceled();
                            _pendingResourceTasks.Remove(handle);
                        }
                        else
                        {
                            Debug.Assert(resource.Source is not null);
                            disposes.Add(resource.Source.Dispose);
                        }

                        _textureArrays[(int)handle.Id] = new BindlessTextureArray();
                        _textureArrayIdFactory.FreeId(handle.Id);
                    }
                        break;
                    case ResourceType.Buffer:
                    {
                        if (ResolveBuffer(handle) is { } buffer) disposes.Add(buffer.Dispose);
                    }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (touchedDescriptors) _resourceDescriptorSet.Update();
            foreach (var dispose in disposes) dispose();
        }
    }

    private void UpdateHandles(params ReadOnlySpan<ResourceHandle> handles)
    {
        List<BindlessResource> pendingToClear = [];
        var touchedDescriptors = false;
        foreach (var handle in handles)
            switch (handle.Type)
            {
                case ResourceType.Texture:
                {
                    var resource = _textures[(int)handle.Id];

                    Debug.Assert(resource.Source is not null);

                    if (handle.IsBindless)
                    {
                        _resourceDescriptorSet.WriteSampledImage(TexturesBinding, resource.Source,
                            ImageLayout.ShaderReadOnly, handle.Id);
                        touchedDescriptors = true;
                    }

                    pendingToClear.Add(resource);
                }
                    break;
                case ResourceType.Cubemap:
                {
                    var resource = _cubemaps[(int)handle.Id];

                    Debug.Assert(resource.Source is not null);

                    if (handle.IsBindless)
                    {
                        _resourceDescriptorSet.WriteSampledCubemap(CubemapsBinding, resource.Source,
                            ImageLayout.ShaderReadOnly, handle.Id);
                        touchedDescriptors = true;
                    }

                    pendingToClear.Add(resource);
                }
                    break;
                case ResourceType.TextureArray:
                {
                    var resource = _textureArrays[(int)handle.Id];

                    Debug.Assert(resource.Source is not null);

                    if (handle.IsBindless)
                    {
                        _resourceDescriptorSet.WriteSampledImageArray(TexturesBinding, resource.Source,
                            ImageLayout.ShaderReadOnly, handle.Id);
                        touchedDescriptors = true;
                    }

                    pendingToClear.Add(resource);
                }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

        if (touchedDescriptors) _resourceDescriptorSet.Update();

        foreach (var resource in pendingToClear)
        {
            if (resource.State == BindlessResourceState.Uploading)
            {
                _pendingResourceTasks[resource.Handle].SetResult(resource.Handle);
                _pendingResourceTasks.Remove(resource.Handle);
            }

            resource.State = BindlessResourceState.Ready;
        }
    }

    public void BindBindlessDescriptors(in VkCommandBuffer cmd)
    {
        cmd.BindDescriptorSets(VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS, _resourcePipelineLayout,
            [_resourceDescriptorSet]);
        cmd.BindDescriptorSets(VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_COMPUTE, _resourcePipelineLayout,
            [_resourceDescriptorSet]);
    }

    public DescriptorSet GetResourceDescriptorSet()
    {
        return _resourceDescriptorSet;
    }

    public VkPipelineLayout GetResourcePipelineLayout()
    {
        return _resourcePipelineLayout;
    }


    private Task<ResourceHandle> HandleAsyncBindless<TBindlessResource, TState>(out ResourceHandle handle,
        List<TBindlessResource> list, TState state,
        Func<TBindlessResource, TaskCompletionSource<ResourceHandle>, TState, Task> createAsyncTask)
        where TBindlessResource : BindlessResource, new()
    {
        var resource = new TBindlessResource
        {
            State = BindlessResourceState.Uploading
        };

        var completionSource = new TaskCompletionSource<ResourceHandle>();

        var idFactory = resource switch
        {
            BindlessTexture => _textureIdFactory,
            BindlessTextureArray => _textureArrayIdFactory,
            BindlessCubemap => _cubemapIdFactory,
            _ => throw new ArgumentOutOfRangeException()
        };

        lock (_resourceSync)
        {
            var id = idFactory.NewId(out var addToArray);
            handle = new ResourceHandle(resource switch
            {
                BindlessTexture => ResourceType.Texture,
                BindlessTextureArray => ResourceType.TextureArray,
                BindlessCubemap => ResourceType.Cubemap,
                _ => throw new ArgumentOutOfRangeException(nameof(list), list, null)
            }, id, true);
            if (addToArray)
                list.Add(resource);
            else
                list[(int)id] = resource;

            _pendingResourceTasks.Add(handle, completionSource);
            resource.Handle = handle;
        }

        var taskState = (resource, completionSource, self: this, state, createAsyncTask);

        ThreadPool.QueueUserWorkItem(static (workData) =>
        {
            try
            {
                workData.createAsyncTask(workData.resource, workData.completionSource, workData.state);
            }
            catch (Exception e)
            {
                lock (workData.self._resourceSync)
                {
                    Console.WriteLine(e);
                    workData.completionSource.SetException(e);
                    workData.self._pendingResourceTasks.Remove(workData.resource.Handle);
                }
            }
        }, taskState, false);

        return completionSource.Task;
    }

    private async Task AsyncCreateTexture(BindlessTexture resource,
        TaskCompletionSource<ResourceHandle> completionSource,
        byte[] rented, int dataLength,
        Extent2D size, ImageFormat format, bool mips = false, ImageUsage usage = ImageUsage.None)
    {
        Task<IDisposableVulkanTexture> task;
        try
        {
            // The upload buffer copy inside CreateVulkanTexture happens synchronously before it
            // returns, so it's safe to release `rented` back to the pool as soon as the call returns.
            task = CreateVulkanTexture(new ReadOnlyMemory<byte>(rented, 0, dataLength), size, format,
                mips, usage | ImageUsage.Sampled);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }

        var image = await task;
        resource.Source = image;
        lock (_resourceSync)
        {
            // In-case the texture was disposed before we finished creating the image
            if (_textures[(int)resource.Handle.Id] != resource)
            {
                if (!completionSource.Task.IsCanceled) completionSource.SetCanceled();
                image.Dispose();
                return;
            }

            resource.Source = image;
            UpdateHandles(resource.Handle);
        }
    }


    private async Task AsyncCreateTextureArray(BindlessTextureArray resource,
        TaskCompletionSource<ResourceHandle> completionSource,
        byte[] rented, int dataLength,
        Extent2D size, ImageFormat format, uint count, bool mips = false, ImageUsage usage = ImageUsage.None)
    {
        Task<IDisposableVulkanTextureArray> task;
        try
        {
            task = CreateVulkanTextureArray(new ReadOnlyMemory<byte>(rented, 0, dataLength), size, format,
                count, mips, usage | ImageUsage.Sampled);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }

        var image = await task;
        resource.Source = image;
        lock (_resourceSync)
        {
            // In-case the texture was disposed before we finished creating the image
            if (_textureArrays[(int)resource.Handle.Id] != resource)
            {
                if (!completionSource.Task.IsCanceled) completionSource.SetCanceled();
                image.Dispose();
                return;
            }

            resource.Source = image;
            UpdateHandles(resource.Handle);
        }
    }

    private async Task AsyncCreateCubemap(BindlessCubemap resource,
        TaskCompletionSource<ResourceHandle> completionSource,
        byte[] rented, int dataLength,
        Extent2D size, ImageFormat format, bool mips = false, ImageUsage usage = ImageUsage.None)
    {
        Task<IDisposableVulkanCubemap> task;
        try
        {
            task = CreateVulkanCubemap(new ReadOnlyMemory<byte>(rented, 0, dataLength), size, format,
                mips, usage | ImageUsage.Sampled);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }

        var image = await task;
        resource.Source = image;
        lock (_resourceSync)
        {
            // In-case the texture was disposed before we finished creating the image
            if (_cubemaps[(int)resource.Handle.Id] != resource)
            {
                if (!completionSource.Task.IsCanceled) completionSource.SetCanceled();
                image.Dispose();
                return;
            }

            resource.Source = image;
            UpdateHandles(resource.Handle);
        }
    }

    // --- Buffer registry ---

    private ResourceHandle RegisterBuffer(IVulkanDeviceBuffer buffer)
    {
        lock (_resourceSync)
        {
            var id = _bufferIdFactory.NewId(out var addToArray);
            if (addToArray)
                _buffers.Add(buffer);
            else
                _buffers[(int)id] = buffer;

            return new ResourceHandle(ResourceType.Buffer, id);
        }
    }

    public IVulkanDeviceBuffer? ResolveBuffer(in ResourceHandle handle)
    {
        if (handle.Type != ResourceType.Buffer || handle.Id == 0 || handle.Id >= _buffers.Count) return null;
        lock (_resourceSync)
        {
            return _buffers[(int)handle.Id];
        }
    }

    private void ReleaseBufferHandle(in ResourceHandle handle)
    {
        if (handle.Type != ResourceType.Buffer || handle.Id == 0) return;
        lock (_resourceSync)
        {
            if (handle.Id < _buffers.Count) _buffers[(int)handle.Id] = null;
            _bufferIdFactory.FreeId(handle.Id);
        }
    }

    public void WriteBuffer(in ResourceHandle handle, ReadOnlySpan<byte> data, ulong offset = 0)
    {
        var buffer = ResolveBuffer(handle) ?? throw new ArgumentException("Invalid buffer handle", nameof(handle));
        unsafe
        {
            fixed (byte* pData = data)
            {
                buffer.WriteRaw(new IntPtr(pData), (ulong)data.Length, offset);
            }
        }
    }

    public ulong GetBufferAddress(in ResourceHandle handle)
    {
        var buffer = ResolveBuffer(handle) ?? throw new ArgumentException("Invalid buffer handle", nameof(handle));
        return buffer.GetAddress();
    }
}