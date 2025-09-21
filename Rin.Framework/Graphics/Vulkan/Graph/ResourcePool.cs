using System.Diagnostics;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Rin.Framework.Extensions;
using Rin.Framework.Graphics.Graph;
using Rin.Framework.Graphics.Images;
using Rin.Framework.Graphics.Vulkan.Images;
using TerraFX.Interop.Vulkan;

namespace Rin.Framework.Graphics.Vulkan.Graph;

public class ResourcePool(WindowRenderer renderer) : IResourcePool
{
    private readonly BufferPool _bufferPool = new();


    private readonly TexturePool _texturePool = new();
    private readonly TextureArrayPool _textureArrayPool = new();
    private readonly CubemapPool _cubemapPool = new();

    private ulong _currentFrame;

    public void Dispose()
    {
        _texturePool.Dispose();
        _textureArrayPool.Dispose();
        _cubemapPool.Dispose();
        _bufferPool.Dispose();
    }
    public IDisposableVulkanTexture CreateTexture(TextureResourceDescriptor descriptor, Frame frame)
    {
        return _texturePool.Create(descriptor, frame,_currentFrame);
    }

    public IDisposableVulkanTextureArray CreateTextureArray(TextureArrayResourceDescriptor descriptor, Frame frame)
    {
        return _textureArrayPool.Create(descriptor, frame,_currentFrame);
    }

    public IDisposableVulkanCubemap CreateCubemap(CubemapResourceDescriptor descriptor, Frame frame)
    {
        return _cubemapPool.Create(descriptor, frame,_currentFrame);
    }

    IVulkanDeviceBuffer IResourcePool.CreateBuffer(BufferResourceDescriptor descriptor, Frame frame)
    {
        return _bufferPool.Create(descriptor, frame, _currentFrame);
    }

    public void OnFrameStart(ulong newFrame)
    {
        _currentFrame = newFrame;
        var framesInFlight = renderer.GetNumFramesInFlight();
        _texturePool.CheckExpiredProxies(newFrame, framesInFlight);
        _textureArrayPool.CheckExpiredProxies(newFrame, framesInFlight);
        _cubemapPool.CheckExpiredProxies(newFrame, framesInFlight);
        _bufferPool.CheckExpiredProxies(newFrame, framesInFlight);
    }

    /// <summary>
    ///     Pooling is done using proxies, i.e. buffer proxies, image proxies. this also means they are not
    /// </summary>
    private interface IContainer : IDisposable
    {
        /// <summary>
        ///     The last frame this proxy was used in
        /// </summary>
        public ulong LastUsed { get; set; }

        /// <summary>
        ///     The frames this proxy is currently used in
        /// </summary>
        public HashSet<Frame> Uses { get; }
    }

    private class ResourceContainer<TResource>(TResource resource) : IContainer where TResource : IDisposable
    {
        [PublicAPI] public TResource Resource => resource;

        public void Dispose()
        {
            Resource.Dispose();
        }

        public ulong LastUsed { get; set; }
        public HashSet<Frame> Uses { get; } = [];
    }

    private abstract class Pool<TResult, TResource, TInput, TPoolKey> : IDisposable
        where TResource : IDisposable
        where TPoolKey : notnull
    {
        [PublicAPI] protected readonly Dictionary<TPoolKey, HashSet<ResourceContainer<TResource>>> ContainerPool = [];

        public void Dispose()
        {
            foreach (var container in ContainerPool.Values.SelectMany(c => c)) container.Dispose();
        }

        protected abstract ResourceContainer<TResource> CreateNew(TInput input, Frame frame, TPoolKey key,
            ulong frameId);

        protected abstract TResult ResultFromContainer(ResourceContainer<TResource> container, Frame frame,
            TPoolKey key, TInput input, ulong frameId);

        protected abstract TPoolKey MakeKeyFromInput(TInput input);

        protected virtual ResourceContainer<TResource>? FindExistingResource(
            Dictionary<TPoolKey, HashSet<ResourceContainer<TResource>>> items, Frame frame, TPoolKey key,
            ulong frameId)
        {
            if (ContainerPool.TryGetValue(key, out var containers))
                foreach (var container in containers)
                    if (!container.Uses.Contains(frame))
                        return container;

            return null;
        }

        public TResult Create(TInput input, Frame frame, ulong frameId)
        {
            var key = MakeKeyFromInput(input);

            {
                if (FindExistingResource(ContainerPool, frame, key, frameId) is { } container)
                {
                    container.LastUsed = frameId;
                    container.Uses.Add(frame);
                    return ResultFromContainer(container, frame, key, input, frameId);
                }
            }

            {
                if (!ContainerPool.ContainsKey(key)) ContainerPool.Add(key, []);

                var created = CreateNew(input, frame, key, frameId);
                created.Uses.Add(frame);
                created.LastUsed = frameId;
                ContainerPool[key].Add(created);

                return ResultFromContainer(created, frame, key, input, frameId);
            }
        }

        public virtual void CheckExpiredProxies(ulong frameId, ulong numFramesInFlight)
        {
            ContainerPool.RemoveWhere((_, containers) =>
            {
                containers.RemoveWhere(container =>
                {
                    if (container.Uses.NotEmpty()) return false;

                    if (container.LastUsed + numFramesInFlight < frameId)
                    {
                        container.Dispose();
                        return true;
                    }

                    return false;
                });

                return containers.Count == 0;
            });
        }
    }

    private sealed class ProxiedTexture(ResourceContainer<IDisposableVulkanTexture> container, Frame frame) : IDisposableVulkanTexture
    {
        public Extent2D Extent => container.Resource.Extent;
        public bool Mips => container.Resource.Mips;
        public ImageFormat Format => container.Resource.Format;
        public ImageHandle Handle => container.Resource.Handle;
        public void Dispose()
        {
            container.Uses.Remove(frame);
        }

        public VkImage VulkanImage => container.Resource.VulkanImage;
        public VkImageView VulkanView => container.Resource.VulkanView;

        public ImageLayout Layout
        {
            get => container.Resource.Layout;
            set => container.Resource.Layout = value;
        }
        
        
        public IntPtr Allocation => container.Resource.Allocation;
    }
    private sealed class ProxiedTextureArray(ResourceContainer<IDisposableVulkanTextureArray> container, Frame frame) : IDisposableVulkanTextureArray
    {
        public Extent2D Extent => container.Resource.Extent;
        public bool Mips => container.Resource.Mips;
        public ImageFormat Format => container.Resource.Format;
        public ImageHandle Handle => container.Resource.Handle;
        public void Dispose()
        {
            container.Uses.Remove(frame);
        }

        public VkImage VulkanImage => container.Resource.VulkanImage;
        public VkImageView VulkanView => container.Resource.VulkanView;

        public ImageLayout Layout
        {
            get => container.Resource.Layout;
            set => container.Resource.Layout = value;
        }
        
        public uint Count =>  container.Resource.Count;
        
        public IntPtr Allocation => container.Resource.Allocation;
    }
    private sealed class ProxiedCubemap(ResourceContainer<IDisposableVulkanCubemap> container, Frame frame) : IDisposableVulkanCubemap
    {
        public Extent2D Extent => container.Resource.Extent;
        public bool Mips => container.Resource.Mips;
        public ImageFormat Format => container.Resource.Format;
        public ImageHandle Handle => container.Resource.Handle;
        public void Dispose()
        {
            container.Uses.Remove(frame);
        }

        public VkImage VulkanImage => container.Resource.VulkanImage;
        public VkImageView VulkanView => container.Resource.VulkanView;

        public ImageLayout Layout
        {
            get => container.Resource.Layout;
            set => container.Resource.Layout = value;
        }
        public IntPtr Allocation => container.Resource.Allocation;
    }

    private sealed class TexturePool : Pool<ProxiedTexture,IDisposableVulkanTexture, TextureResourceDescriptor, int>
    {
        protected override ResourceContainer<IDisposableVulkanTexture> CreateNew(TextureResourceDescriptor input, Frame frame, int key, ulong frameId)
        {
            IDisposableVulkanTexture image;
            if (input.Usage.HasFlag(ImageUsage.Sampled))
            {
                IGraphicsModule.Get().CreateTexture(out var handle, input.Extent, input.Format,false,
                    input.Usage);
                
                var initial = IGraphicsModule.Get().GetTexture(handle);
                
                Debug.Assert(initial is IDisposableVulkanTexture);
                
                image = Unsafe.As<IDisposableVulkanTexture>(initial);
            }
            else
            {
                image  = VulkanGraphicsModule.Get().CreateVulkanTexture(input.Extent, input.Format, false, input.Usage);
            }
            
            return new ResourceContainer<IDisposableVulkanTexture>(image);
        }

        protected override ProxiedTexture ResultFromContainer(ResourceContainer<IDisposableVulkanTexture> container, Frame frame, int key, TextureResourceDescriptor input,
            ulong frameId)
        {
            return new ProxiedTexture(container, frame);
        }

        protected override int MakeKeyFromInput(TextureResourceDescriptor input)
        {
            return input.GetHashCode();
        }
    }
    
    private sealed class TextureArrayPool : Pool<ProxiedTextureArray,IDisposableVulkanTextureArray, TextureArrayResourceDescriptor, int>
    {
        protected override ResourceContainer<IDisposableVulkanTextureArray> CreateNew(TextureArrayResourceDescriptor input, Frame frame, int key, ulong frameId)
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
                image  = VulkanGraphicsModule.Get().CreateVulkanTextureArray(input.Extent, input.Format,input.Count, false, input.Usage);
            }
            
            return new ResourceContainer<IDisposableVulkanTextureArray>(image);
        }

        protected override ProxiedTextureArray ResultFromContainer(ResourceContainer<IDisposableVulkanTextureArray> container, Frame frame, int key, TextureArrayResourceDescriptor input,
            ulong frameId)
        {
            return new ProxiedTextureArray(container, frame);
        }

        protected override int MakeKeyFromInput(TextureArrayResourceDescriptor input)
        {
            return input.GetHashCode();
        }
    }
    
    private sealed class CubemapPool : Pool<ProxiedCubemap,IDisposableVulkanCubemap, CubemapResourceDescriptor, int>
    {
        protected override ResourceContainer<IDisposableVulkanCubemap> CreateNew(CubemapResourceDescriptor input, Frame frame, int key, ulong frameId)
        {
            IDisposableVulkanCubemap image;
            if (input.Usage.HasFlag(ImageUsage.Sampled))
            {
                IGraphicsModule.Get().CreateCubemap(out var handle, input.Extent, input.Format,false,
                    input.Usage);
                
                var initial = IGraphicsModule.Get().GetCubemap(handle);
                
                Debug.Assert(initial is IDisposableVulkanCubemap);
                
                image = Unsafe.As<IDisposableVulkanCubemap>(initial);
            }
            else
            {
                image  = VulkanGraphicsModule.Get().CreateVulkanCubemap(input.Extent, input.Format, false, input.Usage);
            }
            
            return new ResourceContainer<IDisposableVulkanCubemap>(image);
        }

        protected override ProxiedCubemap ResultFromContainer(ResourceContainer<IDisposableVulkanCubemap> container, Frame frame, int key, CubemapResourceDescriptor input,
            ulong frameId)
        {
            return new ProxiedCubemap(container, frame);
        }

        protected override int MakeKeyFromInput(CubemapResourceDescriptor input)
        {
            return input.GetHashCode();
        }
    }

    private sealed class ProxiedBuffer : IVulkanDeviceBuffer
    {
        private readonly ResourceContainer<IVulkanDeviceBuffer> _container;
        private readonly BufferResourceDescriptor _descriptor;
        private readonly Frame _frame;

        public ProxiedBuffer(ResourceContainer<IVulkanDeviceBuffer> container, Frame frame,
            BufferResourceDescriptor descriptor)
        {
            _container = container;
            _frame = frame;
            _descriptor = descriptor;
        }

        public IDeviceBuffer Buffer => _container.Resource;

        public void Dispose()
        {
            _container.Uses.Remove(_frame);
        }

        public ulong Offset => _container.Resource.Offset;
        public ulong Size => _descriptor.Size;
        public VkBuffer NativeBuffer => _container.Resource.NativeBuffer;
        public IntPtr Allocation => _container.Resource.Allocation;

        public ulong GetAddress()
        {
            return _container.Resource.GetAddress();
        }

        public DeviceBufferView GetView(ulong offset, ulong size)
        {
            return new DeviceBufferView(this, Offset + offset, size);
        }

        public void WriteRaw(in IntPtr src, ulong size, ulong offset = 0)
        {
            _container.Resource.WriteRaw(src, size, offset);
        }
    }

    private class BufferPool : Pool<ProxiedBuffer, IVulkanDeviceBuffer, BufferResourceDescriptor, int>
    {
        [PublicAPI] public ulong MaxBufferReuseDelta = 1024;

        protected override ResourceContainer<IVulkanDeviceBuffer> CreateNew(BufferResourceDescriptor input, Frame frame,
            int key, ulong frameId)
        {
            var buffer = VulkanGraphicsModule.Get().NewBuffer(input.Size, input.Usage, false, input.Mapped);
            return new ResourceContainer<IVulkanDeviceBuffer>(buffer);
        }

        protected override ProxiedBuffer ResultFromContainer(ResourceContainer<IVulkanDeviceBuffer> container, Frame frame,
            int key, BufferResourceDescriptor input, ulong frameId)
        {
            return new ProxiedBuffer(container, frame, input);
        }

        protected override int MakeKeyFromInput(BufferResourceDescriptor input)
        {
            return input.GetHashCode();
        }

        protected override ResourceContainer<IVulkanDeviceBuffer>? FindExistingResource(
            Dictionary<int, HashSet<ResourceContainer<IVulkanDeviceBuffer>>> items, Frame frame, int key, ulong frameId)
        {
            return items
                .Where(item => item.Key == key)
                .SelectMany(c => c.Value)
                .FirstOrDefault(c => c.Uses.Empty());
        }
    }
}