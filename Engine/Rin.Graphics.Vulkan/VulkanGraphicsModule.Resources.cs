using System.Diagnostics;
using System.Runtime.InteropServices;
using Rin.Core.Extensions;
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
    private const uint MaxTextures = BindlessData.TextureCount;
    private const uint MaxCubemaps = BindlessData.CubemapCount;
    private const uint MaxTextureArrays = BindlessData.TextureArrayCount;
    private const uint SamplerCount = BindlessData.SamplerCount;
    private const uint SamplersBinding = 0;
    private const uint TexturesBinding = 1;
    private const uint TextureArraysBinding = 2;
    private const uint CubemapsBinding = 3;

    private const uint ResourceDescriptorTotal = MaxTextures + MaxCubemaps + MaxTextureArrays + SamplerCount;

    private readonly Lock _resourceSync = new();

    private const int BufferSlotChunkSize = 1024;
    private const int MaxBufferSlotChunks = 1024;

    private readonly ResourceTable<BindlessTexture> _textures = new(ResourceType.Texture, (int)MaxTextures);
    private readonly ResourceTable<BindlessTextureArray> _textureArrays = new(ResourceType.TextureArray, (int)MaxTextureArrays);
    private readonly ResourceTable<BindlessCubemap> _cubemaps = new(ResourceType.Cubemap, (int)MaxCubemaps);
    private readonly ResourceTable<BindlessBuffer> _buffers = new(ResourceType.Buffer, BufferSlotChunkSize, MaxBufferSlotChunks);

    private readonly UploadLanes _pendingActions = new();
    private readonly UploadBatch _uploads = new();

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

        for (var filter = 0; filter < BindlessData.FilterCount; filter++)
        for (var tiling = 0; tiling < BindlessData.TilingCount; tiling++)
            _resourceDescriptorSet.WriteSampler(SamplersBinding, new SamplerSpec
            {
                Filter = (ImageFilter)filter,
                Tiling = (ImageTiling)tiling
            }, (uint)(filter * BindlessData.TilingCount + tiling));

        _resourceDescriptorSet.Update();
        _resourcePipelineLayout = _device.CreatePipelineLayout([_resourceDescriptorSetLayout]);

        var extent = new Extent2D(1, 1);
        var format = ImageFormat.RGBA8;
        _defaultTexture = CreateVulkanTexture(new Extent2D(DefaultTextureSize, DefaultTextureSize), format,
            usage: ImageCreateFlags.Sampled | ImageCreateFlags.TransferDst);
        _defaultTextureArray = CreateVulkanTextureArray(extent, format, 1, usage: ImageCreateFlags.Sampled);
        _defaultCubemap = CreateVulkanCubemap(extent, format, usage: ImageCreateFlags.Sampled);

        UploadDefaultCheckerboard();

        // Every slot that isn't backed by a live resource yet must still hold a valid descriptor -
        // GPU-Assisted Validation's descriptor-indexing shader instrumentation traps on reads of
        // never-written descriptors even when PartiallyBound would otherwise make them legal.
        for (uint i = 0; i < MaxTextures; i++)
            _resourceDescriptorSet.WriteSampledImage(TexturesBinding, _defaultTexture, ImageLayout.ShaderReadOnly, i);
        for (uint i = 0; i < MaxTextureArrays; i++)
            _resourceDescriptorSet.WriteSampledImageArray(TextureArraysBinding, _defaultTextureArray,
                ImageLayout.ShaderReadOnly, i);
        for (uint i = 0; i < MaxCubemaps; i++)
            _resourceDescriptorSet.WriteSampledCubemap(CubemapsBinding, _defaultCubemap, ImageLayout.ShaderReadOnly,
                i);
        _resourceDescriptorSet.Update();
    }

    private const uint DefaultTextureSize = 8;

    // Black/yellow checkerboard instead of whatever the freshly allocated image memory contains, so a
    // shader that ends up sampling an unbound bindless slot shows an obvious "missing texture" pattern.
    private void UploadDefaultCheckerboard()
    {
        const byte black = 0;
        const byte yellow = 255;
        var pixels = new byte[DefaultTextureSize * DefaultTextureSize * 4];
        for (uint y = 0; y < DefaultTextureSize; y++)
        for (uint x = 0; x < DefaultTextureSize; x++)
        {
            var isYellow = (x + y) % 2 == 0;
            var offset = (y * DefaultTextureSize + x) * 4;
            pixels[offset + 0] = isYellow ? yellow : black;
            pixels[offset + 1] = isYellow ? yellow : black;
            pixels[offset + 2] = black;
            pixels[offset + 3] = 255;
        }

        var dataSize = (ulong)pixels.Length;
        var stagingHandle = CreateBuffer(dataSize, BufferCreateFlags.TransferSrc | BufferCreateFlags.HostDst);
        var uploadBuffer = new DeviceBufferView(stagingHandle, 0, dataSize);
        uploadBuffer.Write(pixels);

        var texture = _defaultTexture;
        var textureArray = _defaultTextureArray;
        var cubemap = _defaultCubemap;
        GraphicsSubmit(ctx =>
        {
            var cmd = ((VulkanExecutionContext)ctx).CommandBuffer;
            cmd.ImageBarrier(texture, ImageLayout.Undefined, ImageLayout.TransferDst);

            var copyRegion = new VkBufferImageCopy
            {
                imageSubresource = new VkImageSubresourceLayers
                {
                    aspectMask = texture.Format.ToAspectFlags(),
                    mipLevel = 0,
                    baseArrayLayer = 0,
                    layerCount = 1
                },
                imageExtent = new VkExtent3D
                {
                    width = texture.Extent.Width,
                    height = texture.Extent.Height,
                    depth = 1
                }
            };
            unsafe
            {
                cmd.CopyBufferToImage(uploadBuffer, texture, new Span<VkBufferImageCopy>(&copyRegion, 1));
            }

            cmd.ImageBarrier(texture, ImageLayout.TransferDst, ImageLayout.ShaderReadOnly);
            cmd.ImageBarrier(textureArray, ImageLayout.Undefined, ImageLayout.ShaderReadOnly);
            cmd.ImageBarrier(cubemap, ImageLayout.Undefined, ImageLayout.ShaderReadOnly);
        }).Then(() => { FreeResourceHandles(uploadBuffer.Buffer); });
    }

    private void DisposeBindlessResources()
    {
        if (_resourceDescriptorAllocator is null) return;

        List<PendingResourceAction> pendingActions = [];
        _pendingActions.TakeAll(pendingActions);
        foreach (var action in pendingActions) action.Dispose();

        _resourceDescriptorAllocator.Dispose();

        // Anything still holding a Source here was never freed via FreeResourceHandles - that's a real
        // leak at the call-site level, so it's worth flagging even though we clean it up here to avoid
        // validation errors on vkDestroyDevice.
        for (var i = 0; i < _textures.Count; i++)
            if (_textures.Get((uint)i)?.Source is { } texture)
            {
                Console.WriteLine($"[Rin.Graphics.Vulkan] Texture at slot {i} was never freed (missing FreeResourceHandles call) - disposing at shutdown.");
                texture.Dispose();
            }

        for (var i = 0; i < _textureArrays.Count; i++)
            if (_textureArrays.Get((uint)i)?.Source is { } textureArray)
            {
                Console.WriteLine($"[Rin.Graphics.Vulkan] TextureArray at slot {i} was never freed (missing FreeResourceHandles call) - disposing at shutdown.");
                textureArray.Dispose();
            }

        for (var i = 0; i < _cubemaps.Count; i++)
            if (_cubemaps.Get((uint)i)?.Source is { } cubemap)
            {
                Console.WriteLine($"[Rin.Graphics.Vulkan] Cubemap at slot {i} was never freed (missing FreeResourceHandles call) - disposing at shutdown.");
                cubemap.Dispose();
            }

        for (var i = 0; i < _buffers.Count; i++)
            if (_buffers.Get((uint)i)?.Source is { } buffer)
            {
                Console.WriteLine($"[Rin.Graphics.Vulkan] Buffer at slot {i} was never freed (missing FreeResourceHandles call) - disposing at shutdown.");
                buffer.Dispose();
            }

        _defaultTexture.Dispose();
        _defaultCubemap.Dispose();
        _defaultTextureArray.Dispose();
        _device.DestroyPipelineLayout(_resourcePipelineLayout);
    }

    public ResourceHandle CreateTexture(in Extent2D size, ImageFormat format, bool mips = false,
        ImageCreateFlags usage = ImageCreateFlags.None)
    {
        ThrowIfEmpty(size);
        var image = CreateVulkanTexture(size, format, mips, usage);
        var isBindless = usage.HasFlag(ImageCreateFlags.Sampled);

        lock (_resourceSync)
        {
            var (id, generation) = _textures.Reserve();
            var handle = new ResourceHandle(ResourceType.Texture, id, isBindless, generation);
            if (image is VulkanTexture concreteImage) concreteImage.Handle = handle;
            var resource = new BindlessTexture
            {
                Handle = handle,
                Source = image,
                State = BindlessResourceState.PendingBind,
                Generation = generation
            };

            _textures.Set(id, resource);

            UpdateHandles(resource.Handle);

            return resource.Handle;
        }
    }

    public ResourceHandle CreateTextureArray(in Extent2D size, ImageFormat format, uint count, bool mips = false,
        ImageCreateFlags usage = ImageCreateFlags.None)
    {
        ThrowIfEmpty(size);
        var image = CreateVulkanTextureArray(size, format, count, mips, usage);
        var isBindless = usage.HasFlag(ImageCreateFlags.Sampled);

        lock (_resourceSync)
        {
            var (id, generation) = _textureArrays.Reserve();
            var handle = new ResourceHandle(ResourceType.TextureArray, id, isBindless, generation);
            if (image is VulkanTextureArray concreteImage) concreteImage.Handle = handle;
            var resource = new BindlessTextureArray
            {
                Handle = handle,
                Source = image,
                State = BindlessResourceState.PendingBind,
                Generation = generation
            };

            _textureArrays.Set(id, resource);

            UpdateHandles(resource.Handle);

            return resource.Handle;
        }
    }

    public ResourceHandle CreateCubemap(in Extent2D size, ImageFormat format, bool mips = false,
        ImageCreateFlags usage = ImageCreateFlags.None)
    {
        ThrowIfEmpty(size);
        var image = CreateVulkanCubemap(size, format, mips, usage);
        var isBindless = usage.HasFlag(ImageCreateFlags.Sampled);

        lock (_resourceSync)
        {
            var (id, generation) = _cubemaps.Reserve();
            var handle = new ResourceHandle(ResourceType.Cubemap, id, isBindless, generation);
            if (image is VulkanCubemap concreteImage) concreteImage.Handle = handle;
            var resource = new BindlessCubemap
            {
                Handle = handle,
                Source = image,
                State = BindlessResourceState.PendingBind,
                Generation = generation
            };

            _cubemaps.Set(id, resource);

            UpdateHandles(resource.Handle);

            return resource.Handle;
        }
    }

    public Task<ResourceHandle> CreateTexture(out ResourceHandle handle, ReadOnlySpan<byte> data, in Extent2D size,
        ImageFormat format, bool mips = false, ImageCreateFlags usage = ImageCreateFlags.None)
    {
        Debug.Assert(size.Width * size.Height * format.PixelByteSize() == (ulong)data.Length,
            "Unexpected image buffer size");
        var image = CreateVulkanTexture(size, format, mips,
            usage | ImageCreateFlags.Sampled | ImageCreateFlags.TransferDst);

        lock (_resourceSync)
        {
            var (id, generation) = _textures.Reserve();
            handle = new ResourceHandle(ResourceType.Texture, id, true, generation);
            if (image is VulkanTexture concreteImage) concreteImage.Handle = handle;
            var resource = new BindlessTexture
            {
                Handle = handle,
                Source = image,
                State = BindlessResourceState.Ready,
                Generation = generation,
                DescriptorPending = true
            };

            _textures.Set(id, resource);
        }

        _pendingActions.EnqueueTexture(handle,
            new PendingTextureWrite(PooledMemory<byte>.CopyFrom(data), default, size, size));
        return Task.FromResult(handle);
    }

    public Task<ResourceHandle> CreateTextureArray(out ResourceHandle handle, ReadOnlySpan<byte> data,
        in Extent2D size,
        ImageFormat format, uint count, bool mips = false, ImageCreateFlags usage = ImageCreateFlags.None)
    {
        throw new NotImplementedException();
    }

    public Task<ResourceHandle> CreateCubemap(out ResourceHandle handle, ReadOnlySpan<byte> data, in Extent2D size,
        ImageFormat format, bool mips = false, ImageCreateFlags usage = ImageCreateFlags.None)
    {
        throw new NotImplementedException();
    }

    public Task QueueTextureUpload(ResourceHandle handle, ReadOnlyMemory<byte> data, Extent2D extent,
        Offset2D offset = default)
    {
        Debug.Assert(handle.IsValid(), "Handle is invalid");
        var texture = GetTexture(handle) ?? throw new ArgumentException("Invalid or unresolvable texture handle",
            nameof(handle));
        Debug.Assert(extent.Width * extent.Height * texture.Format.PixelByteSize() == (ulong)data.Length,
            "Unexpected image buffer size");

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var write = new PendingTextureWrite(PooledMemory<byte>.CopyFrom(data.Span), offset, extent,
            texture.Extent);
        write.Tasks.Add(completion);
        _pendingActions.EnqueueTexture(handle, write);
        return completion.Task;
    }

    internal UploadLanes PendingActions => _pendingActions;

    private ResourceHandle AllocateStaging(ulong size)
    {
        return CreateBuffer(size, BufferCreateFlags.TransferSrc | BufferCreateFlags.HostDst);
    }

    ResourceHandle IStagingAllocator.Allocate(ulong size) => AllocateStaging(size);

    void IStagingAllocator.Free(in ResourceHandle handle) => FreeResourceHandles(handle);

    IVulkanTexture? IExternalResourceSource.GetTexture(in ResourceHandle handle) => GetTexture(handle);

    IVulkanCubemap? IExternalResourceSource.GetCubemap(in ResourceHandle handle) => GetCubemap(handle);

    IVulkanTextureArray? IExternalResourceSource.GetTextureArray(in ResourceHandle handle) =>
        GetTextureArray(handle);

    IVulkanDeviceBuffer? IExternalResourceSource.ResolveBuffer(in ResourceHandle handle) => ResolveBuffer(handle);

    public Task QueueTextureCopy(ResourceHandle src, ResourceHandle dst, Offset2D srcOffset, Offset2D dstOffset,
        Extent2D extent)
    {
        var source = GetTexture(src) ?? throw new ArgumentException("Invalid or unresolvable texture handle", nameof(src));
        var destination = GetTexture(dst) ??
                          throw new ArgumentException("Invalid or unresolvable texture handle", nameof(dst));
        ThrowIfSameResource(src, dst);
        Debug.Assert(source.Usage.HasFlag(ImageCreateFlags.TransferSrc),
            "Copy source texture was not created with ImageCreateFlags.TransferSrc");
        Debug.Assert(srcOffset.X + extent.Width <= source.Extent.Width &&
                     srcOffset.Y + extent.Height <= source.Extent.Height, "Copy runs past the source texture");
        Debug.Assert(dstOffset.X + extent.Width <= destination.Extent.Width &&
                     dstOffset.Y + extent.Height <= destination.Extent.Height, "Copy runs past the target texture");
        if (extent.Width == 0 || extent.Height == 0) return Task.CompletedTask;

        return EnqueueCopy(new TextureCopy(src, dst, srcOffset, dstOffset, extent));
    }

    public Task QueueBufferToTextureCopy(ResourceHandle src, ResourceHandle dst, ulong srcOffset, Offset2D dstOffset,
        Extent2D extent)
    {
        var source = ResolveBuffer(src) ?? throw new ArgumentException("Invalid or unresolvable buffer handle", nameof(src));
        var destination = GetTexture(dst) ??
                          throw new ArgumentException("Invalid or unresolvable texture handle", nameof(dst));
        var size = extent.Width * extent.Height * destination.Format.PixelByteSize();
        Debug.Assert(srcOffset + size <= source.Size, "Copy runs past the end of the buffer");
        Debug.Assert(dstOffset.X + extent.Width <= destination.Extent.Width &&
                     dstOffset.Y + extent.Height <= destination.Extent.Height, "Copy runs past the target texture");
        if (extent.Width == 0 || extent.Height == 0) return Task.CompletedTask;

        return EnqueueCopy(new BufferToTextureCopy(src, dst, srcOffset, size, dstOffset, extent));
    }

    public Task QueueTextureToBufferCopy(ResourceHandle src, ResourceHandle dst, Offset2D srcOffset, Extent2D extent,
        ulong dstOffset)
    {
        var source = GetTexture(src) ?? throw new ArgumentException("Invalid or unresolvable texture handle", nameof(src));
        var destination = ResolveBuffer(dst) ??
                          throw new ArgumentException("Invalid or unresolvable buffer handle", nameof(dst));
        var size = extent.Width * extent.Height * source.Format.PixelByteSize();
        Debug.Assert(source.Usage.HasFlag(ImageCreateFlags.TransferSrc),
            "Copy source texture was not created with ImageCreateFlags.TransferSrc");
        Debug.Assert(srcOffset.X + extent.Width <= source.Extent.Width &&
                     srcOffset.Y + extent.Height <= source.Extent.Height, "Copy runs past the source texture");
        Debug.Assert(dstOffset + size <= destination.Size, "Copy runs past the end of the buffer");
        if (extent.Width == 0 || extent.Height == 0) return Task.CompletedTask;

        return EnqueueCopy(new TextureToBufferCopy(src, dst, srcOffset, extent, dstOffset, size));
    }

    private Task EnqueueCopy(CopyAction copy)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var shared = new SharedWrite(copy);
        shared.Tasks.Add(completion);
        _pendingActions.EnqueueShared(shared);
        return completion.Task;
    }

    private static void ThrowIfSameResource(in ResourceHandle src, in ResourceHandle dst)
    {
        if (src.Equals(dst))
            throw new ArgumentException("A texture cannot be copied onto itself", nameof(dst));
    }

    internal void BindPendingTexture(in ResourceHandle handle)
    {
        lock (_resourceSync)
        {
            if (FindResource(handle) is not BindlessTexture { DescriptorPending: true } resource) return;
            resource.DescriptorPending = false;
            _resourceDescriptorSet.WriteSampledImage(TexturesBinding, resource.Source!, ImageLayout.ShaderReadOnly,
                handle.Id);
            _resourceDescriptorSet.Update();
        }
    }

    // The rest waits for later frames.
    private const int MaxFlushedActions = 64;

    private void FlushPendingWrites()
    {
        _uploads.Fill(_pendingActions, MaxFlushedActions, this);
        if (_uploads.IsEmpty) return;

        if (_uploads.StagingSize > 0)
        {
            var staging = AllocateStaging(_uploads.StagingSize);
            SubmitGraphicsAndWait(cmd => RecordUploads(cmd, staging));
            FreeResourceHandles(staging);
        }

        _uploads.CompleteWrites();
        foreach (var handle in _uploads.Acquired) ReleaseResource(handle);
        _uploads.Clear();
    }

    private void RecordUploads(in VkCommandBuffer cmd, in ResourceHandle staging)
    {
        ulong offset = 0;
        foreach (var step in _uploads.Steps)
            switch (step.Kind)
            {
                case UploadBatchStepKind.TextureWrite:
                {
                    var write = step.TextureWrite!;
                    var image = GetTexture(step.Handle)!;
                    cmd.ImageBarrier(image, write.CoversWholeImage ? ImageLayout.Undefined : image.Layout,
                        ImageLayout.TransferDst);
                    WriteRecorder.RecordTextureCopy(cmd, image,
                        new DeviceBufferView(staging, offset, write.StagingSize), write);
                    offset += write.StagingSize;
                    BindPendingTexture(step.Handle);
                    break;
                }
                case UploadBatchStepKind.BufferWrites:
                {
                    var size = WriteRecorder.StagingSize(step.BufferWrites);
                    WriteRecorder.RecordBufferWrites(cmd, step.Handle, new DeviceBufferView(staging, offset, size),
                        step.BufferWrites);
                    offset += size;
                    break;
                }
                case UploadBatchStepKind.Copy:
                    WriteRecorder.RecordCopy(cmd, step.Shared!.Action);
                    break;
            }

        foreach (var step in _uploads.Steps)
        {
            var texture = step.WrittenTexture;
            if (texture.Id != 0 && GetTexture(texture) is { Layout: ImageLayout.TransferDst } image)
                cmd.ImageBarrier(image, ImageLayout.TransferDst, ImageLayout.ShaderReadOnly);
        }
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
            var (id, generation) = _textures.Reserve();
            var handle = new ResourceHandle(ResourceType.Texture, id, isBindless, generation);
            var resource = new BindlessTexture
            {
                Handle = handle,
                Source = image,
                State = BindlessResourceState.Ready,
                Generation = generation
            };

            _textures.Set(id, resource);

            if (isBindless) UpdateHandles(handle);

            return handle;
        }
    }

    public IDisposableVulkanTexture? GetTexture(in ResourceHandle handle) => _textures.Resolve(handle);

    public IDisposableVulkanTextureArray? GetTextureArray(in ResourceHandle handle) =>
        _textureArrays.Resolve(handle);

    public IDisposableVulkanCubemap? GetCubemap(in ResourceHandle handle) => _cubemaps.Resolve(handle);

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

    public ResourceHandle ResolveResourceHandle(DeviceHandle device)
    {
        BindlessResource? resource = device.Id == 0
            ? null
            : device.Type switch
            {
                ResourceType.Texture => _textures.Get(device.Id),
                ResourceType.Cubemap => _cubemaps.Get(device.Id),
                ResourceType.TextureArray => _textureArrays.Get(device.Id),
                ResourceType.Buffer => _buffers.Get(device.Id),
                _ => null
            };
        return resource is { Retired: false, Handle.Id: > 0 } ? resource.Handle : new ResourceHandle(device.Type, 0);
    }

    public void FreeResourceHandles(params ReadOnlySpan<ResourceHandle> handles)
    {
        lock (_resourceSync)
        {
            List<ResourceHandle>? toDestroy = null;
            foreach (var handle in handles)
            {
                if (FindResource(handle) is not { Retired: false } resource) continue;
                resource.Retired = true;
                if (--resource.References == 0) (toDestroy ??= []).Add(handle);
            }

            if (toDestroy is not null) DestroyResources(CollectionsMarshal.AsSpan(toDestroy));
        }
    }

    public bool TryAcquireResource(in ResourceHandle handle)
    {
        lock (_resourceSync)
        {
            if (FindResource(handle) is not { Retired: false, State: BindlessResourceState.Ready } resource)
                return false;
            resource.References++;
            return true;
        }
    }

    public void ReleaseResource(in ResourceHandle handle)
    {
        lock (_resourceSync)
        {
            if (FindResource(handle) is not { } resource) return;
            if (--resource.References == 0) DestroyResources([handle]);
        }
    }

    private BindlessResource? FindResource(in ResourceHandle handle)
    {
        return handle.Type switch
        {
            ResourceType.Texture => _textures.Find(handle),
            ResourceType.Cubemap => _cubemaps.Find(handle),
            ResourceType.TextureArray => _textureArrays.Find(handle),
            ResourceType.Buffer => _buffers.Find(handle),
            _ => null
        };
    }

    private void DestroyResources(ReadOnlySpan<ResourceHandle> handles)
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

                        if (_textures.Free(handle) is not { } resource) break;
                        Debug.Assert(resource.Source is not null);
                        disposes.Add(resource.Source.Dispose);
                        _pendingActions.Drop(handle);
                    }
                        break;
                    case ResourceType.Cubemap:
                    {
                        if (handle.IsBindless)
                        {
                            _resourceDescriptorSet.WriteSampledCubemap(CubemapsBinding, _defaultCubemap,
                                ImageLayout.ShaderReadOnly, handle.Id);
                            touchedDescriptors = true;
                        }

                        if (_cubemaps.Free(handle) is not { } resource) break;
                        Debug.Assert(resource.Source is not null);
                        disposes.Add(resource.Source.Dispose);
                    }
                        break;
                    case ResourceType.TextureArray:
                    {
                        if (handle.IsBindless)
                        {
                            _resourceDescriptorSet.WriteSampledImageArray(TextureArraysBinding, _defaultTextureArray,
                                ImageLayout.ShaderReadOnly, handle.Id);
                            touchedDescriptors = true;
                        }

                        if (_textureArrays.Free(handle) is not { } resource) break;
                        Debug.Assert(resource.Source is not null);
                        disposes.Add(resource.Source.Dispose);
                    }
                        break;
                    case ResourceType.Buffer:
                    {
                        if (_buffers.Free(handle) is not { } resource) break;
                        if (resource.Source is { } buffer) disposes.Add(buffer.Dispose);
                        _pendingActions.Drop(handle);
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
                    var resource = _textures.Get(handle.Id)!;

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
                    var resource = _cubemaps.Get(handle.Id)!;

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
                    var resource = _textureArrays.Get(handle.Id)!;

                    Debug.Assert(resource.Source is not null);

                    if (handle.IsBindless)
                    {
                        _resourceDescriptorSet.WriteSampledImageArray(TextureArraysBinding, resource.Source,
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

        foreach (var resource in pendingToClear) resource.State = BindlessResourceState.Ready;
    }

    // Bound once per frame - every material/mesh shader needs it.
    public void BindBindlessDescriptors(in VkCommandBuffer cmd)
    {
        cmd.BindDescriptorSets(VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS, _resourcePipelineLayout,
            [_resourceDescriptorSet]);
    }

    // Not bound at frame start (unlike the graphics bind point) because most compute passes never
    // touch bindless resources, and binding this descriptor set to VK_PIPELINE_BIND_POINT_COMPUTE
    // unconditionally reproduced a GPU-AV descriptor-indexing device-lost on every compute dispatch,
    // regardless of what the dispatched shader actually did. A compute shader that genuinely needs
    // bindless textures should call this itself, right before its own dispatch.
    public void BindBindlessComputeDescriptors(in VkCommandBuffer cmd)
    {
        cmd.BindDescriptorSets(VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_COMPUTE, _resourcePipelineLayout,
            [_resourceDescriptorSet]);
    }

    public const string GlobalBindlessBlockName = BindlessData.Name;

    /// <summary>
    ///     The engine-owned descriptor set layout a shader's <c>[BindlessBlock(name)]</c> resolves to, or
    ///     null if no block is registered under that name. The shader's own layout for that set is
    ///     replaced by this one rather than built from reflection, so the block's declaration in the
    ///     shader and the engine's table can't drift apart silently.
    /// </summary>
    public VkDescriptorSetLayout? FindBindlessBlockLayout(string name)
    {
        return name == GlobalBindlessBlockName ? _resourceDescriptorSetLayout : null;
    }

    public DescriptorSet GetResourceDescriptorSet()
    {
        return _resourceDescriptorSet;
    }

    public VkPipelineLayout GetResourcePipelineLayout()
    {
        return _resourcePipelineLayout;
    }

    private static void ThrowIfEmpty(in Extent2D size)
    {
        if (size.Width == 0 || size.Height == 0)
            throw new ArgumentOutOfRangeException(nameof(size), size, "Image dimensions must be greater than zero");
    }

    // --- Buffer registry ---

    public ResourceHandle CreateBuffer(ulong size, BufferCreateFlags flags, bool sequentialWrite = true)
    {
        ArgumentOutOfRangeException.ThrowIfZero(size);
        var hostVisible = flags.IsHostVisible();
        var buffer = NewBuffer(size, flags.ToVkUsage(), flags.ToVkMemoryProperty(), sequentialWrite,
            false, hostVisible, "Buffer");

        lock (_resourceSync)
        {
            var (id, generation) = _buffers.Reserve();
            var handle = new ResourceHandle(ResourceType.Buffer, id, false, generation);
            if (buffer is VulkanDeviceBuffer concreteBuffer) concreteBuffer.Handle = handle;

            var resource = new BindlessBuffer
            {
                Handle = handle,
                Source = buffer,
                HostVisible = hostVisible,
                State = BindlessResourceState.Ready,
                Generation = generation
            };

            _buffers.Set(id, resource);

            return handle;
        }
    }

    public Task QueueBufferUpload(ResourceHandle handle, ReadOnlyMemory<byte> data, ulong offset = 0)
    {
        Debug.Assert(handle.IsValid(), "Handle is invalid");
        var buffer = ResolveBuffer(handle) ?? throw new ArgumentException("Invalid or unresolvable buffer handle",
            nameof(handle));
        Debug.Assert(offset + (ulong)data.Length <= buffer.Size, "Upload runs past the end of the buffer");
        if (data.IsEmpty) return Task.CompletedTask;

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var write = new PendingBufferWrite(PooledMemory<byte>.CopyFrom(data.Span), offset);
        write.Tasks.Add(completion);
        _pendingActions.EnqueueBuffer(handle, write);
        return completion.Task;
    }

    public Task QueueBufferCopy(ResourceHandle src, ResourceHandle dst, ulong srcOffset, ulong dstOffset, ulong size)
    {
        var source = ResolveBuffer(src) ?? throw new ArgumentException("Invalid or unresolvable buffer handle", nameof(src));
        var destination = ResolveBuffer(dst) ??
                          throw new ArgumentException("Invalid or unresolvable buffer handle", nameof(dst));
        Debug.Assert(srcOffset + size <= source.Size, "Copy runs past the end of the source buffer");
        Debug.Assert(dstOffset + size <= destination.Size, "Copy runs past the end of the target buffer");
        Debug.Assert(!src.Equals(dst) || srcOffset + size <= dstOffset || dstOffset + size <= srcOffset,
            "Regions of a copy within one buffer must not overlap");
        if (size == 0) return Task.CompletedTask;

        return EnqueueCopy(new BufferCopy(src, dst, srcOffset, dstOffset, size));
    }

    public IVulkanDeviceBuffer? ResolveBuffer(in ResourceHandle handle) => _buffers.Resolve(handle)?.Source;

    public void WriteBuffer(in ResourceHandle handle, ReadOnlySpan<byte> data, ulong offset = 0)
    {
        if (handle.Type != ResourceType.Buffer || handle.Id == 0 || _buffers.Get(handle.Id) is not { } resource)
            throw new ArgumentException("Invalid buffer handle", nameof(handle));
        if (resource.Generation != handle.Generation)
            throw new ArgumentException("Stale buffer handle - resource has been freed", nameof(handle));

        if (!resource.HostVisible)
            throw new InvalidOperationException(
                "Cannot WriteBuffer a buffer that wasn't created with BufferCreateFlags.HostSrc/HostDst - use QueueBufferUpload instead");

        var buffer = resource.Source ?? throw new ArgumentException("Invalid buffer handle", nameof(handle));
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

    public void SetDebugName(in ResourceHandle handle, string name)
    {
        var allocation = handle.Type == ResourceType.Buffer
            ? ResolveBuffer(handle)?.Allocation
            : GetImage(handle)?.Allocation;

        if (allocation is not { } value)
            throw new ArgumentException("Invalid or unresolvable resource handle", nameof(handle));

        Native.allocatorSetAllocationName(_allocator, value, name);
    }
}