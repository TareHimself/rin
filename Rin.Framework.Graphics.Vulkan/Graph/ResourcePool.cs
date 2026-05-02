using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Rin.Framework.Graphics.Images;
using Rin.Framework.Graphics.Vulkan.Images;
using TerraFX.Interop.Vulkan;

namespace Rin.Framework.Graphics.Vulkan.Graph;

public class ResourcePool : IResourcePool
{
    private const float ExpiredResourceTimeoutSeconds = 4.0f;

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
        var time = IApplication.Get().TimeSeconds;
        _texturePool.CheckExpiredProxies(time);
        _textureArrayPool.CheckExpiredProxies(time);
        _cubemapPool.CheckExpiredProxies(time);
        _bufferPool.CheckExpiredProxies(time);
    }


    private abstract class Pool<TResult, TResource, TInput, TPoolKey> : IDisposable
        where TResource : class, IDisposable
        where TPoolKey : notnull
    {
        [PublicAPI]
        protected readonly Dictionary<TPoolKey, LinkedList<KeyValuePair<float, TResource>>> ContainerPool = [];

        public void Dispose()
        {
            foreach (var container in ContainerPool.Values.SelectMany(c => c)) container.Value.Dispose();
        }

        protected abstract TResource CreateNew(TInput input, TPoolKey key);

        protected abstract TResult MakeResult(LinkedList<KeyValuePair<float, TResource>> container, TResource resource,
            TPoolKey key, TInput input);

        protected abstract TPoolKey MakeKeyFromInput(TInput input);

        protected virtual bool FindExistingResource(TPoolKey key,
            TInput input,
            [NotNullWhen(true)] out TResource? resource,
            [NotNullWhen(true)] out LinkedList<KeyValuePair<float, TResource>>? container)
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

        public virtual void CheckExpiredProxies(float time)
        {
            foreach (var list in ContainerPool.Values)
                while (list.Last is not null)
                {
                    var (timeAdded, resource) = list.Last.Value;
                    var delta = time - timeAdded;
                    if (delta > ExpiredResourceTimeoutSeconds)
                    {
                        resource.Dispose();
                        list.RemoveLast();
                        Debug.WriteLine($"Removing resource from pool {GetType().Name}");
                    }
                    else
                    {
                        break;
                    }
                }
        }
    }

    private sealed class ProxiedTexture(
        LinkedList<KeyValuePair<float, IDisposableVulkanTexture>> container,
        IDisposableVulkanTexture resource)
        : IDisposableVulkanTexture
    {
        public Extent2D Extent => resource.Extent;
        public bool Mips => resource.Mips;
        public ImageFormat Format => resource.Format;
        public ImageHandle Handle => resource.Handle;

        public void Dispose()
        {
            container.AddFirst(
                new KeyValuePair<float, IDisposableVulkanTexture>(IApplication.Get().TimeSeconds, resource));
        }

        public VkImage VulkanImage => resource.VulkanImage;
        public VkImageView VulkanView => resource.VulkanView;

        public ImageLayout Layout
        {
            get => resource.Layout;
            set => resource.Layout = value;
        }


        public IntPtr Allocation => resource.Allocation;
    }

    private sealed class ProxiedTextureArray(
        LinkedList<KeyValuePair<float, IDisposableVulkanTextureArray>> container,
        IDisposableVulkanTextureArray resource)
        : IDisposableVulkanTextureArray
    {
        public Extent2D Extent => resource.Extent;
        public bool Mips => resource.Mips;
        public ImageFormat Format => resource.Format;
        public ImageHandle Handle => resource.Handle;

        public void Dispose()
        {
            container.AddFirst(
                new KeyValuePair<float, IDisposableVulkanTextureArray>(IApplication.Get().TimeSeconds, resource));
        }

        public VkImage VulkanImage => resource.VulkanImage;
        public VkImageView VulkanView => resource.VulkanView;

        public ImageLayout Layout
        {
            get => resource.Layout;
            set => resource.Layout = value;
        }

        public uint Count => resource.Count;

        public IntPtr Allocation => resource.Allocation;
    }

    private sealed class ProxiedCubemap(
        LinkedList<KeyValuePair<float, IDisposableVulkanCubemap>> container,
        IDisposableVulkanCubemap resource)
        : IDisposableVulkanCubemap
    {
        public Extent2D Extent => resource.Extent;
        public bool Mips => resource.Mips;
        public ImageFormat Format => resource.Format;
        public ImageHandle Handle => resource.Handle;

        public void Dispose()
        {
            container.AddFirst(
                new KeyValuePair<float, IDisposableVulkanCubemap>(IApplication.Get().TimeSeconds, resource));
        }

        public VkImage VulkanImage => resource.VulkanImage;
        public VkImageView VulkanView => resource.VulkanView;

        public ImageLayout Layout
        {
            get => resource.Layout;
            set => resource.Layout = value;
        }

        public IntPtr Allocation => resource.Allocation;
    }

    private sealed class TexturePool : Pool<ProxiedTexture, IDisposableVulkanTexture, TextureResourceDescriptor, int>
    {
        protected override IDisposableVulkanTexture CreateNew(TextureResourceDescriptor input,
            int key)
        {
            Debug.WriteLine($"Creating resource for pool {nameof(TexturePool)}");
            IDisposableVulkanTexture image;
            if (input.Usage.HasFlag(ImageUsage.Sampled))
            {
                IGraphicsModule.Get().CreateTexture(out var handle, input.Extent, input.Format, false,
                    input.Usage);

                var initial = IGraphicsModule.Get().GetTexture(handle);

                Debug.Assert(initial is IDisposableVulkanTexture);

                image = Unsafe.As<IDisposableVulkanTexture>(initial);
            }
            else
            {
                image = VulkanGraphicsModule.Get().CreateVulkanTexture(input.Extent, input.Format, false, input.Usage);
            }

            return image;
        }

        protected override ProxiedTexture MakeResult(
            LinkedList<KeyValuePair<float, IDisposableVulkanTexture>> container,
            IDisposableVulkanTexture resource,
            int key, TextureResourceDescriptor input)
        {
            return new ProxiedTexture(container, resource);
        }

        protected override int MakeKeyFromInput(TextureResourceDescriptor input)
        {
            return input.GetHashCode();
        }
    }

    private sealed class TextureArrayPool : Pool<ProxiedTextureArray, IDisposableVulkanTextureArray,
        TextureArrayResourceDescriptor, int>
    {
        protected override IDisposableVulkanTextureArray CreateNew(TextureArrayResourceDescriptor input,
            int key)
        {
            IDisposableVulkanTextureArray image;
            if (input.Usage.HasFlag(ImageUsage.Sampled))
            {
                IGraphicsModule.Get().CreateTextureArray(out var handle, input.Extent, input.Format, input.Count, false,
                    input.Usage);

                var initial = IGraphicsModule.Get().GetTextureArray(handle);

                Debug.Assert(initial is IDisposableVulkanTextureArray);

                image = Unsafe.As<IDisposableVulkanTextureArray>(initial);
            }
            else
            {
                image = VulkanGraphicsModule.Get()
                    .CreateVulkanTextureArray(input.Extent, input.Format, input.Count, false, input.Usage);
            }

            return image;
        }

        protected override ProxiedTextureArray MakeResult(
            LinkedList<KeyValuePair<float, IDisposableVulkanTextureArray>> container,
            IDisposableVulkanTextureArray resource, int key,
            TextureArrayResourceDescriptor input)
        {
            return new ProxiedTextureArray(container, resource);
        }

        protected override int MakeKeyFromInput(TextureArrayResourceDescriptor input)
        {
            return input.GetHashCode();
        }
    }

    private sealed class CubemapPool : Pool<ProxiedCubemap, IDisposableVulkanCubemap, CubemapResourceDescriptor, int>
    {
        protected override IDisposableVulkanCubemap CreateNew(CubemapResourceDescriptor input,
            int key)
        {
            IDisposableVulkanCubemap image;
            if (input.Usage.HasFlag(ImageUsage.Sampled))
            {
                IGraphicsModule.Get().CreateCubemap(out var handle, input.Extent, input.Format, false,
                    input.Usage);

                var initial = IGraphicsModule.Get().GetCubemap(handle);

                Debug.Assert(initial is IDisposableVulkanCubemap);

                image = Unsafe.As<IDisposableVulkanCubemap>(initial);
            }
            else
            {
                image = VulkanGraphicsModule.Get().CreateVulkanCubemap(input.Extent, input.Format, false, input.Usage);
            }

            return image;
        }

        protected override ProxiedCubemap MakeResult(
            LinkedList<KeyValuePair<float, IDisposableVulkanCubemap>> container,
            IDisposableVulkanCubemap resource,
            int key, CubemapResourceDescriptor input)
        {
            return new ProxiedCubemap(container, resource);
        }

        protected override int MakeKeyFromInput(CubemapResourceDescriptor input)
        {
            return input.GetHashCode();
        }
    }

    private sealed class ProxiedBuffer : IVulkanDeviceBuffer
    {
        private readonly LinkedList<KeyValuePair<float, BufferContainer>> _container;
        private readonly BufferContainer _resource;

        public ProxiedBuffer(LinkedList<KeyValuePair<float, BufferContainer>> container, BufferContainer resource,
            ulong? size = null)
        {
            _container = container;
            _resource = resource;
            Size = size ?? _resource.Descriptor.Size;
        }

        [PublicAPI] public IDeviceBuffer Buffer => _resource.Buffer;

        public void Dispose()
        {
            _container.AddFirst(new KeyValuePair<float, BufferContainer>(IApplication.Get().TimeSeconds, _resource));
        }

        public ulong Offset => _resource.Buffer.Offset;
        public ulong Size { get; }
        public VkBuffer NativeBuffer => _resource.Buffer.NativeBuffer;
        public IntPtr Allocation => _resource.Buffer.Allocation;

        public ulong GetAddress()
        {
            return _resource.Buffer.GetAddress();
        }

        public DeviceBufferView GetView(ulong offset, ulong size)
        {
            return new DeviceBufferView(this, Offset + offset, size);
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
            buffer.Dispose();
        }
    }

    private class BufferPool : Pool<ProxiedBuffer, BufferContainer, BufferResourceDescriptor, int>
    {
        [PublicAPI] public ulong MaxBufferReuseDelta = 1024;

        protected override BufferContainer CreateNew(BufferResourceDescriptor input,
            int key)
        {
            Debug.WriteLine($"Creating resource for pool {GetType().Name}");
            var buffer = VulkanGraphicsModule.Get().NewBuffer(input.Size, input.Usage, false, input.Mapped);
            return new BufferContainer(buffer, input);
        }

        protected override ProxiedBuffer MakeResult(LinkedList<KeyValuePair<float, BufferContainer>> container,
            BufferContainer resource,
            int key, BufferResourceDescriptor input)
        {
            return new ProxiedBuffer(container, resource, input.Size);
        }

        protected override int MakeKeyFromInput(BufferResourceDescriptor input)
        {
            return input.GetHashCode();
        }

        protected override bool FindExistingResource(int key, BufferResourceDescriptor input,
            [NotNullWhen(true)] out BufferContainer? resource,
            [NotNullWhen(true)] out LinkedList<KeyValuePair<float, BufferContainer>>? container)
        {
            var result = ContainerPool
                .Where(item => item.Value.First is not null)
                .Select(c => c.Value)
                .FirstOrDefault(item =>
                {
                    var buffContainer = item.First!.Value.Value;

                    if (buffContainer.Buffer.Size >= input.Size && buffContainer.Descriptor.Usage == input.Usage &&
                        buffContainer.Descriptor.Mapped == input.Mapped)
                    {
                        var delta = buffContainer.Buffer.Size - input.Size;
                        return delta <= MaxBufferReuseDelta;
                    }

                    return false;
                });

            if (result?.First is not null)
            {
                container = result;
                resource = container.First.Value.Value;
                result.RemoveFirst();
                return true;
            }

            resource = null;
            container = null;
            return false;
        }
    }
}