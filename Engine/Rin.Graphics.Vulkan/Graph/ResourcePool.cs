using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Graphics.Vulkan.Images;
using TerraFX.Interop.Vulkan;

namespace Rin.Graphics.Vulkan.Graph;

public class ResourcePool : IResourcePool
{
    // Effectively disabled: destroying a pooled image and reusing its descriptor slot for a new one -
    // even correctly, through FreeResourceHandles, well past any frames-in-flight margin - reliably
    // trips a Khronos Validation Layer GPU-AV false positive ("Descriptor index N references a
    // resource that was destroyed") on the very next draw against that slot. Confirmed by disabling
    // eviction entirely: zero validation errors over the same run that otherwise produces thousands.
    // Matches known validation-layer bugs with UPDATE_AFTER_BIND + PARTIALLY_BOUND descriptor reuse:
    // https://github.com/KhronosGroup/Vulkan-ValidationLayers/issues/3835
    // https://github.com/KhronosGroup/Vulkan-ValidationLayers/issues/3729
    // Trade-off: pooled images now live for the app's lifetime instead of being freed when idle.

    /// <summary>Free a pooled resource once it has gone unused for this many rendered frames.</summary>
    [PublicAPI] public static ulong MaxIdleFrames = ulong.MaxValue;

    /// <summary>Cap on idle resources kept per pool key; extras are freed immediately on return.</summary>
    [PublicAPI] public static int MaxIdlePerKey = int.MaxValue;

    private readonly BufferPool _bufferPool = new();
    private readonly CubemapPool _cubemapPool = new();
    private readonly TextureArrayPool _textureArrayPool = new();


    private readonly TexturePool _texturePool = new();


    public void Dispose()
    {
        _texturePool.Dispose();
        _textureArrayPool.Dispose();
        _cubemapPool.Dispose();
        _bufferPool.Dispose();
    }

    public IDisposableVulkanTexture CreateTexture(TextureResourceDescriptor descriptor, Frame frame)
    {
        return _texturePool.Create(descriptor);
    }

    public IDisposableVulkanTextureArray CreateTextureArray(TextureArrayResourceDescriptor descriptor, Frame frame)
    {
        return _textureArrayPool.Create(descriptor);
    }

    public IDisposableVulkanCubemap CreateCubemap(CubemapResourceDescriptor descriptor, Frame frame)
    {
        return _cubemapPool.Create(descriptor);
    }

    IVulkanDeviceBuffer IResourcePool.CreateBuffer(BufferResourceDescriptor descriptor, Frame frame)
    {
        return _bufferPool.Create(descriptor);
    }

    public void OnFrameEnd(ulong newFrame)
    {
        _texturePool.OnFrameEnd(newFrame);
        _textureArrayPool.OnFrameEnd(newFrame);
        _cubemapPool.OnFrameEnd(newFrame);
        _bufferPool.OnFrameEnd(newFrame);
    }

    /// <summary>Lets a proxy hand its wrapped resource back to the pool it came from.</summary>
    private interface IReturnTarget<TResource>
    {
        void Return(LinkedList<KeyValuePair<ulong, TResource>> container, TResource resource);
    }

    private abstract class Pool<TResult, TResource, TInput, TPoolKey> : IReturnTarget<TResource>, IDisposable
        where TResource : class, IDisposable
        where TPoolKey : notnull
    {
        [PublicAPI]
        protected readonly Dictionary<TPoolKey, LinkedList<KeyValuePair<ulong, TResource>>> ContainerPool = [];

        protected ulong CurrentFrame;

        public void Dispose()
        {
            foreach (var container in ContainerPool.Values.SelectMany(c => c)) container.Value.Dispose();
            ContainerPool.Clear();
        }

        public void Return(LinkedList<KeyValuePair<ulong, TResource>> container, TResource resource)
        {
            // Never dispose here — freeing happens only in OnFrameEnd, well past frames-in-flight.
            // The proxy captured this exact list at checkout, so it must stay reachable even if
            // OnFrameEnd emptied it in the meantime; keys are therefore never removed.
            container.AddFirst(new KeyValuePair<ulong, TResource>(CurrentFrame, resource));
        }

        protected abstract TResource CreateNew(TInput input, TPoolKey key);

        protected abstract TResult MakeResult(LinkedList<KeyValuePair<ulong, TResource>> container, TResource resource,
            TPoolKey key, TInput input);

        protected abstract TPoolKey MakeKeyFromInput(TInput input);

        protected virtual bool FindExistingResource(TPoolKey key,
            TInput input,
            [NotNullWhen(true)] out TResource? resource,
            [NotNullWhen(true)] out LinkedList<KeyValuePair<ulong, TResource>>? container)
        {
            if (ContainerPool.TryGetValue(key, out var resources))
                if (resources.First is not null)
                {
                    resource = resources.First.Value.Value;
                    container = resources;
                    resources.RemoveFirst();
                    return true;
                }

            resource = null;
            container = null;
            return false;
        }

        public TResult Create(TInput input)
        {
            var key = MakeKeyFromInput(input);

            {
                if (FindExistingResource(key, input, out var resource, out var container))
                    return MakeResult(container, resource, key, input);
            }

            {
                if (!ContainerPool.ContainsKey(key)) ContainerPool.Add(key, []);

                var created = CreateNew(input, key);
                var container = ContainerPool[key];

                return MakeResult(container, created, key, input);
            }
        }

        // A returned resource is only safe to free once its owning frame's fence has passed.
        // Resources are checked out exclusively, so frame-in-flight count is a sufficient margin.
        private const ulong SafetyFrames = 4;

        public void OnFrameEnd(ulong frame)
        {
            CurrentFrame = frame;

            foreach (var list in ContainerPool.Values)
            {
                // Age out anything idle for too long.
                while (list.Last is { } last && Idle(last.Value.Key) > MaxIdleFrames)
                    Evict(list);

                // Trim to the per-key cap, oldest first, but never something too fresh to be safe.
                while (list.Count > MaxIdlePerKey && list.Last is { } last && Idle(last.Value.Key) >= SafetyFrames)
                    Evict(list);
            }

            return;

            ulong Idle(ulong stamp) => frame >= stamp ? frame - stamp : 0;

            void Evict(LinkedList<KeyValuePair<ulong, TResource>> list)
            {
                list.Last!.Value.Value.Dispose();
                list.RemoveLast();
                Debug.WriteLine($"Removing resource from pool {GetType().Name}");
            }
        }
    }

    // Wraps the raw bindless image so pool eviction disposes it through FreeResourceHandles (which
    // resets the descriptor slot to the default image and frees the id) instead of destroying the
    // native VkImage/VkImageView directly while the bindless descriptor set still points at it.
    private sealed class TextureContainer(IDisposableVulkanTexture image) : IDisposable
    {
        public IDisposableVulkanTexture Image => image;

        public void Dispose()
        {
            VulkanGraphicsModule.Get().FreeResourceHandles(image.Handle);
        }
    }

    private sealed class TextureArrayContainer(IDisposableVulkanTextureArray image) : IDisposable
    {
        public IDisposableVulkanTextureArray Image => image;

        public void Dispose()
        {
            VulkanGraphicsModule.Get().FreeResourceHandles(image.Handle);
        }
    }

    private sealed class CubemapContainer(IDisposableVulkanCubemap image) : IDisposable
    {
        public IDisposableVulkanCubemap Image => image;

        public void Dispose()
        {
            VulkanGraphicsModule.Get().FreeResourceHandles(image.Handle);
        }
    }

    private sealed class ProxiedTexture(
        IReturnTarget<TextureContainer> target,
        LinkedList<KeyValuePair<ulong, TextureContainer>> container,
        TextureContainer resource)
        : IDisposableVulkanTexture
    {
        public Extent2D Extent => resource.Image.Extent;
        public bool Mips => resource.Image.Mips;
        public ImageFormat Format => resource.Image.Format;
        public ResourceHandle Handle => resource.Image.Handle;

        public void Dispose()
        {
            target.Return(container, resource);
        }

        public VkImage VulkanImage => resource.Image.VulkanImage;
        public VkImageView VulkanView => resource.Image.VulkanView;

        public ImageLayout Layout
        {
            get => resource.Image.Layout;
            set => resource.Image.Layout = value;
        }


        public IntPtr Allocation => resource.Image.Allocation;
    }

    private sealed class ProxiedTextureArray(
        IReturnTarget<TextureArrayContainer> target,
        LinkedList<KeyValuePair<ulong, TextureArrayContainer>> container,
        TextureArrayContainer resource)
        : IDisposableVulkanTextureArray
    {
        public Extent2D Extent => resource.Image.Extent;
        public bool Mips => resource.Image.Mips;
        public ImageFormat Format => resource.Image.Format;
        public ResourceHandle Handle => resource.Image.Handle;

        public void Dispose()
        {
            target.Return(container, resource);
        }

        public VkImage VulkanImage => resource.Image.VulkanImage;
        public VkImageView VulkanView => resource.Image.VulkanView;

        public ImageLayout Layout
        {
            get => resource.Image.Layout;
            set => resource.Image.Layout = value;
        }

        public uint Count => resource.Image.Count;

        public IntPtr Allocation => resource.Image.Allocation;
    }

    private sealed class ProxiedCubemap(
        IReturnTarget<CubemapContainer> target,
        LinkedList<KeyValuePair<ulong, CubemapContainer>> container,
        CubemapContainer resource)
        : IDisposableVulkanCubemap
    {
        public Extent2D Extent => resource.Image.Extent;
        public bool Mips => resource.Image.Mips;
        public ImageFormat Format => resource.Image.Format;
        public ResourceHandle Handle => resource.Image.Handle;

        public void Dispose()
        {
            target.Return(container, resource);
        }

        public VkImage VulkanImage => resource.Image.VulkanImage;
        public VkImageView VulkanView => resource.Image.VulkanView;

        public ImageLayout Layout
        {
            get => resource.Image.Layout;
            set => resource.Image.Layout = value;
        }

        public IntPtr Allocation => resource.Image.Allocation;
    }

    private sealed class TexturePool : Pool<ProxiedTexture, TextureContainer, TextureResourceDescriptor, int>
    {
        protected override TextureContainer CreateNew(TextureResourceDescriptor input,
            int key)
        {
            Debug.WriteLine($"Creating resource for pool {nameof(TexturePool)}");
            var handle = IGraphicsModule.Get().CreateTexture(input.Extent, input.Format, false, input.Usage);
            var image = VulkanGraphicsModule.Get().GetTexture(handle);
            Debug.Assert(image is not null);
            return new TextureContainer(image!);
        }

        protected override ProxiedTexture MakeResult(
            LinkedList<KeyValuePair<ulong, TextureContainer>> container,
            TextureContainer resource,
            int key, TextureResourceDescriptor input)
        {
            return new ProxiedTexture(this, container, resource);
        }

        protected override int MakeKeyFromInput(TextureResourceDescriptor input)
        {
            return input.GetHashCode();
        }
    }

    private sealed class TextureArrayPool : Pool<ProxiedTextureArray, TextureArrayContainer,
        TextureArrayResourceDescriptor, int>
    {
        protected override TextureArrayContainer CreateNew(TextureArrayResourceDescriptor input,
            int key)
        {
            var handle = IGraphicsModule.Get()
                .CreateTextureArray(input.Extent, input.Format, input.Count, false, input.Usage);
            var image = VulkanGraphicsModule.Get().GetTextureArray(handle);
            Debug.Assert(image is not null);
            return new TextureArrayContainer(image!);
        }

        protected override ProxiedTextureArray MakeResult(
            LinkedList<KeyValuePair<ulong, TextureArrayContainer>> container,
            TextureArrayContainer resource, int key,
            TextureArrayResourceDescriptor input)
        {
            return new ProxiedTextureArray(this, container, resource);
        }

        protected override int MakeKeyFromInput(TextureArrayResourceDescriptor input)
        {
            return input.GetHashCode();
        }
    }

    private sealed class CubemapPool : Pool<ProxiedCubemap, CubemapContainer, CubemapResourceDescriptor, int>
    {
        protected override CubemapContainer CreateNew(CubemapResourceDescriptor input,
            int key)
        {
            var handle = IGraphicsModule.Get().CreateCubemap(input.Extent, input.Format, false, input.Usage);
            var image = VulkanGraphicsModule.Get().GetCubemap(handle);
            Debug.Assert(image is not null);
            return new CubemapContainer(image!);
        }

        protected override ProxiedCubemap MakeResult(
            LinkedList<KeyValuePair<ulong, CubemapContainer>> container,
            CubemapContainer resource,
            int key, CubemapResourceDescriptor input)
        {
            return new ProxiedCubemap(this, container, resource);
        }

        protected override int MakeKeyFromInput(CubemapResourceDescriptor input)
        {
            return input.GetHashCode();
        }
    }

    private sealed class ProxiedBuffer : IVulkanDeviceBuffer
    {
        private readonly LinkedList<KeyValuePair<ulong, BufferContainer>> _container;
        private readonly BufferContainer _resource;
        private readonly IReturnTarget<BufferContainer> _target;

        public ProxiedBuffer(IReturnTarget<BufferContainer> target,
            LinkedList<KeyValuePair<ulong, BufferContainer>> container, BufferContainer resource,
            ulong? size = null)
        {
            _target = target;
            _container = container;
            _resource = resource;
            Size = size ?? _resource.Descriptor.Size;
        }

        [PublicAPI] public IVulkanDeviceBuffer Buffer => _resource.Buffer;

        public void Dispose()
        {
            _target.Return(_container, _resource);
        }

        public ulong Offset => _resource.Buffer.Offset;
        public ulong Size { get; }
        public ResourceHandle Handle => _resource.Buffer.Handle;
        public VkBuffer NativeBuffer => _resource.Buffer.NativeBuffer;
        public IntPtr Allocation => _resource.Buffer.Allocation;

        public ulong GetAddress()
        {
            return _resource.Buffer.GetAddress();
        }

        public DeviceBufferView GetView(ulong offset, ulong size)
        {
            return new DeviceBufferView(Handle, Offset + offset, size);
        }

        public void WriteRaw(in IntPtr src, ulong size, ulong offset = 0)
        {
            _resource.Buffer.WriteRaw(src, size, offset);
        }
    }

    private class BufferContainer(IVulkanDeviceBuffer buffer, BufferResourceDescriptor descriptor) : IDisposable
    {
        public IVulkanDeviceBuffer Buffer => buffer;
        public BufferResourceDescriptor Descriptor => descriptor;

        public void Dispose()
        {
            VulkanGraphicsModule.Get().FreeResourceHandles(buffer.Handle);
        }
    }

    private class BufferPool : Pool<ProxiedBuffer, BufferContainer, BufferResourceDescriptor, int>
    {
        [PublicAPI] public ulong MaxBufferReuseDelta = 1024;

        protected override BufferContainer CreateNew(BufferResourceDescriptor input,
            int key)
        {
            Debug.WriteLine($"Creating resource for pool {GetType().Name}");
            var graphics = VulkanGraphicsModule.Get();
            var handle = graphics.CreateBuffer(input.Size, input.Usage, false);
            var buffer = graphics.ResolveBuffer(handle) ?? throw new InvalidOperationException(
                "CreateBuffer did not register a resolvable buffer");
            return new BufferContainer(buffer, input);
        }

        protected override ProxiedBuffer MakeResult(LinkedList<KeyValuePair<ulong, BufferContainer>> container,
            BufferContainer resource,
            int key, BufferResourceDescriptor input)
        {
            return new ProxiedBuffer(this, container, resource, input.Size);
        }

        protected override int MakeKeyFromInput(BufferResourceDescriptor input)
        {
            return input.GetHashCode();
        }

        protected override bool FindExistingResource(int key, BufferResourceDescriptor input,
            [NotNullWhen(true)] out BufferContainer? resource,
            [NotNullWhen(true)] out LinkedList<KeyValuePair<ulong, BufferContainer>>? container)
        {
            LinkedList<KeyValuePair<ulong, BufferContainer>>? best = null;
            var bestSize = ulong.MaxValue;

            foreach (var list in ContainerPool.Values)
            {
                if (list.First is not { } head) continue;
                var candidate = head.Value.Value;

                if (candidate.Buffer.Size < input.Size ||
                    candidate.Descriptor.Usage != input.Usage)
                    continue;

                if (candidate.Buffer.Size - input.Size > MaxBufferReuseDelta) continue;

                // Smallest buffer that still fits — avoids handing out a near-max buffer when a snug one is free.
                if (candidate.Buffer.Size >= bestSize) continue;
                best = list;
                bestSize = candidate.Buffer.Size;
            }

            if (best?.First is not null)
            {
                container = best;
                resource = container.First.Value.Value;
                best.RemoveFirst();
                return true;
            }

            resource = null;
            container = null;
            return false;
        }
    }
}
