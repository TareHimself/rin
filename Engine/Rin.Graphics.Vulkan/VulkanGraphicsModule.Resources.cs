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

    private const int BufferSlotChunkSize = 1024;
    private const int MaxBufferSlotChunks = 1024;

    private readonly ResourceSlots<BindlessTexture> _textures = new((int)MaxTextures);
    private readonly ResourceSlots<BindlessTextureArray> _textureArrays = new((int)MaxTextureArrays);
    private readonly ResourceSlots<BindlessCubemap> _cubemaps = new((int)MaxCubemaps);
    private readonly ResourceSlots<BindlessBuffer> _buffers = new(BufferSlotChunkSize, MaxBufferSlotChunks);

    private readonly TextureWriteLanes _textureWrites = new();
    private readonly BufferWriteLanes _bufferWrites = new();

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
        _textures.Set(0, new BindlessTexture());
        _textureArrays.Set(0, new BindlessTextureArray());
        _cubemaps.Set(0, new BindlessCubemap());
        _buffers.Set(0, new BindlessBuffer());

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

        List<(ResourceHandle Handle, PendingTextureWrite Write)> pendingTextureWrites = [];
        _textureWrites.TakeAll(pendingTextureWrites);
        foreach (var (_, write) in pendingTextureWrites) write.Dispose();
        List<(ResourceHandle Handle, PendingBufferWrite Write)> pendingBufferWrites = [];
        _bufferWrites.TakeAll(pendingBufferWrites);
        foreach (var (_, write) in pendingBufferWrites) write.Dispose();

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

    /// <summary>
    ///     The generation the next occupant of slot <paramref name="id" /> should be stamped with - one past
    ///     whatever the slot's previous occupant (if any) last held. Must be read before the slot is
    ///     overwritten.
    /// </summary>
    private static uint NextGeneration<T>(ResourceSlots<T> slots, uint id, bool isNewSlot) where T : BindlessResource
    {
        return isNewSlot ? 0u : unchecked(slots.Get(id)!.Generation + 1);
    }

    public ResourceHandle CreateTexture(in Extent2D size, ImageFormat format, bool mips = false,
        ImageCreateFlags usage = ImageCreateFlags.None)
    {
        var image = CreateVulkanTexture(size, format, mips, usage);
        var isBindless = usage.HasFlag(ImageCreateFlags.Sampled);

        lock (_resourceSync)
        {
            var id = _textureIdFactory.NewId(out var addToArray);
            var generation = NextGeneration(_textures, id, addToArray);
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
        var image = CreateVulkanTextureArray(size, format, count, mips, usage);
        var isBindless = usage.HasFlag(ImageCreateFlags.Sampled);

        lock (_resourceSync)
        {
            var id = _textureArrayIdFactory.NewId(out var addToArray);
            var generation = NextGeneration(_textureArrays, id, addToArray);
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
        var image = CreateVulkanCubemap(size, format, mips, usage);
        var isBindless = usage.HasFlag(ImageCreateFlags.Sampled);

        lock (_resourceSync)
        {
            var id = _cubemapIdFactory.NewId(out var addToArray);
            var generation = NextGeneration(_cubemaps, id, addToArray);
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
            var id = _textureIdFactory.NewId(out var addToArray);
            var generation = NextGeneration(_textures, id, addToArray);
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

        _textureWrites.Enqueue(handle,
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
        _textureWrites.Enqueue(handle, write);
        return completion.Task;
    }

    internal int PeekTextureWrites(in ResourceHandle handle, out bool firstCoversWholeImage)
    {
        var count = _textureWrites.Peek(handle, out var first);
        firstCoversWholeImage = first?.CoversWholeImage ?? false;
        return count;
    }

    internal void TakeTextureWrites(in ResourceHandle handle, int max, List<PendingTextureWrite> into)
    {
        _textureWrites.TryTake(handle, max, into);
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

    // Per lane kind, so a burst of one can't starve the other; the rest waits for later frames.
    private const int MaxFlushedWritesPerKind = 64;

    private void FlushPendingWrites()
    {
        List<(ResourceHandle Handle, PendingTextureWrite Write)> pendingTextures = [];
        List<(ResourceHandle Handle, PendingBufferWrite Write)> pendingBuffers = [];
        _textureWrites.TakeUpTo(MaxFlushedWritesPerKind, pendingTextures);
        _bufferWrites.TakeUpTo(MaxFlushedWritesPerKind, pendingBuffers);
        if (pendingTextures.Count == 0 && pendingBuffers.Count == 0) return;

        HashSet<ResourceHandle> acquired = [];
        HashSet<ResourceHandle> refused = [];

        bool Acquire(ResourceHandle handle, PendingWrite write)
        {
            if (acquired.Contains(handle)) return true;
            if (!refused.Contains(handle) && TryAcquireResource(handle))
            {
                acquired.Add(handle);
                return true;
            }

            refused.Add(handle);
            write.Dispose();
            return false;
        }

        List<(ResourceHandle Handle, PendingTextureWrite Write)> textureWrites = [];
        Dictionary<ResourceHandle, List<PendingBufferWrite>> bufferWrites = [];
        ulong stagingSize = 0;
        foreach (var (handle, write) in pendingTextures)
        {
            if (!Acquire(handle, write)) continue;
            textureWrites.Add((handle, write));
            stagingSize += write.StagingSize;
        }

        foreach (var (handle, write) in pendingBuffers)
        {
            if (!Acquire(handle, write)) continue;
            if (!bufferWrites.TryGetValue(handle, out var writes)) bufferWrites[handle] = writes = [];
            writes.Add(write);
            stagingSize += write.StagingSize;
        }

        if (stagingSize == 0) return;

        var staging = WriteRecorder.CreateStaging(this, stagingSize);
        SubmitGraphicsAndWait(cmd =>
        {
            ulong offset = 0;
            foreach (var (handle, write) in textureWrites)
            {
                var image = GetTexture(handle)!;
                cmd.ImageBarrier(image, write.CoversWholeImage ? ImageLayout.Undefined : image.Layout,
                    ImageLayout.TransferDst);
                WriteRecorder.RecordTextureCopy(cmd, image, new DeviceBufferView(staging, offset, write.StagingSize),
                    write);
                offset += write.StagingSize;
                BindPendingTexture(handle);
            }

            foreach (var (handle, _) in textureWrites)
                if (GetTexture(handle) is { Layout: ImageLayout.TransferDst } image)
                    cmd.ImageBarrier(image, ImageLayout.TransferDst, ImageLayout.ShaderReadOnly);

            foreach (var (handle, writes) in bufferWrites)
            {
                var size = WriteRecorder.StagingSize(writes);
                WriteRecorder.RecordBufferWrites(cmd, handle, new DeviceBufferView(staging, offset, size), writes);
                offset += size;
            }
        });

        foreach (var (_, write) in textureWrites)
        {
            write.Complete();
            write.Dispose();
        }

        foreach (var write in bufferWrites.Values.SelectMany(w => w))
        {
            write.Complete();
            write.Dispose();
        }

        FreeResourceHandles(staging);
        foreach (var handle in acquired) ReleaseResource(handle);
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
            var generation = NextGeneration(_textures, id, addToArray);
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

    public IDisposableVulkanTexture? GetTexture(in ResourceHandle handle)
    {
        if (handle.Type != ResourceType.Texture || handle.Id == 0) return null;
        return _textures.Get(handle.Id) is { State: BindlessResourceState.Ready } resource &&
               resource.Generation == handle.Generation
            ? resource
            : null;
    }

    public IDisposableVulkanTextureArray? GetTextureArray(in ResourceHandle handle)
    {
        if (handle.Type != ResourceType.TextureArray || handle.Id == 0) return null;
        return _textureArrays.Get(handle.Id) is { State: BindlessResourceState.Ready } resource &&
               resource.Generation == handle.Generation
            ? resource
            : null;
    }

    public IDisposableVulkanCubemap? GetCubemap(in ResourceHandle handle)
    {
        if (handle.Type != ResourceType.Cubemap || handle.Id == 0) return null;
        return _cubemaps.Get(handle.Id) is { State: BindlessResourceState.Ready } resource &&
               resource.Generation == handle.Generation
            ? resource
            : null;
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
        BindlessResource? resource = handle.Type switch
        {
            ResourceType.Texture => _textures.Get(handle.Id),
            ResourceType.Cubemap => _cubemaps.Get(handle.Id),
            ResourceType.TextureArray => _textureArrays.Get(handle.Id),
            ResourceType.Buffer => _buffers.Get(handle.Id),
            _ => null
        };
        return handle.Id != 0 && resource?.Generation == handle.Generation ? resource : null;
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

                        var resource = _textures.Get(handle.Id)!;
                        Debug.Assert(resource.Source is not null);
                        disposes.Add(resource.Source.Dispose);

                        _textures.Set(handle.Id, new BindlessTexture { Generation = unchecked(resource.Generation + 1) });
                        _textureIdFactory.FreeId(handle.Id);
                        _textureWrites.Drop(handle);
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

                        var resource = _cubemaps.Get(handle.Id)!;
                        Debug.Assert(resource.Source is not null);
                        disposes.Add(resource.Source.Dispose);

                        _cubemaps.Set(handle.Id, new BindlessCubemap { Generation = unchecked(resource.Generation + 1) });
                        _cubemapIdFactory.FreeId(handle.Id);
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

                        var resource = _textureArrays.Get(handle.Id)!;
                        Debug.Assert(resource.Source is not null);
                        disposes.Add(resource.Source.Dispose);

                        _textureArrays.Set(handle.Id, new BindlessTextureArray { Generation = unchecked(resource.Generation + 1) });
                        _textureArrayIdFactory.FreeId(handle.Id);
                    }
                        break;
                    case ResourceType.Buffer:
                    {
                        if (_buffers.Get(handle.Id) is not { } resource || resource.Generation != handle.Generation)
                            break;

                        if (resource.Source is { } buffer) disposes.Add(buffer.Dispose);

                        _buffers.Set(handle.Id, new BindlessBuffer { Generation = unchecked(resource.Generation + 1) });
                        _bufferIdFactory.FreeId(handle.Id);
                        _bufferWrites.Drop(handle);
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

    public const string GlobalBindlessBlockName = "rin.global";

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

    // --- Buffer registry ---

    public ResourceHandle CreateBuffer(ulong size, BufferCreateFlags flags, bool sequentialWrite = true)
    {
        var hostVisible = flags.IsHostVisible();
        var buffer = NewBuffer(size, flags.ToVkUsage(), flags.ToVkMemoryProperty(), sequentialWrite,
            false, hostVisible, "Buffer");

        lock (_resourceSync)
        {
            var id = _bufferIdFactory.NewId(out var addToArray);
            var generation = NextGeneration(_buffers, id, addToArray);
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
        _bufferWrites.Enqueue(handle, write);
        return completion.Task;
    }

    internal bool HasBufferWrites(in ResourceHandle handle)
    {
        return _bufferWrites.Peek(handle, out _) > 0;
    }

    internal void TakeBufferWrites(in ResourceHandle handle, List<PendingBufferWrite> into)
    {
        _bufferWrites.TryTake(handle, int.MaxValue, into);
    }

    public IVulkanDeviceBuffer? ResolveBuffer(in ResourceHandle handle)
    {
        if (handle.Type != ResourceType.Buffer || handle.Id == 0) return null;
        return _buffers.Get(handle.Id) is { State: BindlessResourceState.Ready } resource &&
               resource.Generation == handle.Generation
            ? resource.Source
            : null;
    }

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