using JetBrains.Annotations;
using rin.Framework.Core.Extensions;
using TerraFX.Interop.Vulkan;

namespace rin.Framework.Graphics.FrameGraph;

public class ResourcePool(WindowRenderer renderer) : IResourcePool
{
    
    /// <summary>
    /// Pooling is done using proxies, i.e. buffer proxies, image proxies. this also means they are not 
    /// </summary>
    private interface IContainer : IDisposable
    {
        /// <summary>
        /// The last frame this proxy was used in
        /// </summary>
        public ulong LastUsed { get; set; }
        
        /// <summary>
        /// The frames this proxy is currently used in
        /// </summary>
        public HashSet<Frame> Uses { get; }
    }
    
    private class ResourceContainer<TResource>(TResource resource) : IContainer where TResource : IDisposable
    {
        public void Dispose()
        {
            Resource.Dispose();
        }

        public ulong LastUsed { get; set; }
        public HashSet<Frame> Uses { get; } = [];

        [PublicAPI] public TResource Resource => resource;
    }
    
    private abstract class Pool<TResult,TResource,TInput,TPoolKey> : IDisposable 
        where TResource : IDisposable
        where TPoolKey : notnull
    {
        [PublicAPI]
        protected readonly Dictionary<TPoolKey, HashSet<ResourceContainer<TResource>>> ContainerPool = [];
        
        protected abstract ResourceContainer<TResource> CreateNew(TInput input,Frame frame,TPoolKey key,ulong frameId);
        
        protected abstract TResult ResultFromContainer(ResourceContainer<TResource> container,Frame frame,TPoolKey key,ulong frameId);
        
        protected abstract TPoolKey MakeKeyFromInput(TInput input);

        protected virtual ResourceContainer<TResource>? FindExistingResource(
            Dictionary<TPoolKey, HashSet<ResourceContainer<TResource>>> items, Frame frame, TPoolKey key,
            ulong frameId)
        {
            if (ContainerPool.TryGetValue(key, out var containers))
            {
                foreach (var container in containers)
                {
                    if (!container.Uses.Contains(frame))
                    {
                        return container;
                    }
                }
            }

            return null;
        }

        public TResult Create(TInput input, Frame frame,ulong frameId)
        {
            
            var key = MakeKeyFromInput(input);

            {
                if (FindExistingResource(ContainerPool, frame, key, frameId) is {} container)
                {
                    container.LastUsed = frameId;
                    container.Uses.Add(frame);
                    return ResultFromContainer(container, frame, key, frameId);
                }
            }

            {
                if (!ContainerPool.ContainsKey(key))
                {
                    ContainerPool.Add(key,[]);
                }
            
                var created = CreateNew(input, frame, key, frameId);
                created.Uses.Add(frame);
                created.LastUsed = frameId;
                ContainerPool[key].Add(created);

                return ResultFromContainer(created, frame, key, frameId);
            }
        }

        public virtual void CheckExpiredProxies(ulong frameId, ulong numFramesInFlight)
        {
            foreach (var containers in ContainerPool.Values)
            {
                containers.RemoveWhere((container) =>
                {
                    if (container.Uses.NotEmpty())
                    {
                        return false;
                    }

                    if (container.LastUsed + numFramesInFlight < frameId)
                    {
                        container.Dispose();
                        return true;
                    }

                    return false;
                });
            }
        }

        public void Dispose()
        {
            foreach (var container in ContainerPool.Values.SelectMany(c => c))
            {
                container.Dispose();
            }
        }
    }
    
    private sealed class ProxiedImage(ResourceContainer<IDeviceImage> container,Frame frame) : IDeviceImage
    {
        public void Dispose()
        {
            container.Uses.Remove(frame);
        }

        public ImageFormat Format => container.Resource.Format;
        public VkExtent3D Extent => container.Resource.Extent;
        public VkImage NativeImage => container.Resource.NativeImage;
        public VkImageView NativeView { get; set; } = container.Resource.NativeView;
    }
    
    private sealed class ImagePool : Pool<ProxiedImage,IDeviceImage,ImageResourceDescriptor,int>
    {
        protected override ResourceContainer<IDeviceImage> CreateNew(ImageResourceDescriptor input, Frame frame, int key, ulong frameId)
        {
            var image = SGraphicsModule.Get().CreateImage(new VkExtent3D()
            {
                width = input.Width,
                height = input.Height,
                depth = 1
            },input.Format,input.Flags,debugName: "Frame Graph Image");
            return new ResourceContainer<IDeviceImage>(image);
        }

        protected override ProxiedImage ResultFromContainer(ResourceContainer<IDeviceImage> container, Frame frame, int key, ulong frameId) => new ProxiedImage(container, frame);

        protected override int MakeKeyFromInput(ImageResourceDescriptor input) => input.GetHashCode();
    }
    

    private sealed class ProxiedBuffer(ResourceContainer<IDeviceBuffer> container,Frame frame) : IDeviceBuffer
    {
        public void Dispose()
        {
            container.Uses.Remove(frame);
        }

        public ulong Offset => container.Resource.Offset;
        public ulong Size => container.Resource.Size;
        public VkBuffer NativeBuffer => container.Resource.NativeBuffer;
        public ulong GetAddress() => container.Resource.GetAddress();

        public IDeviceBufferView GetView(ulong offset, ulong size) => container.Resource.GetView(offset, size);

        public unsafe void Write(void* src, ulong size, ulong offset = 0) => container.Resource.Write(src, size, offset);
    }
    
    private class BufferPool : Pool<ProxiedBuffer,IDeviceBuffer,BufferResourceDescriptor,ulong>
    {
        [PublicAPI]
        public ulong MaxBufferReuseDelta = 1024;
        protected override ResourceContainer<IDeviceBuffer> CreateNew(BufferResourceDescriptor input, Frame frame, ulong key, ulong frameId)
        {
            var buffer = SGraphicsModule.Get().NewStorageBuffer(input.Size,false,"Frame Graph Storage Buffer");
            return new ResourceContainer<IDeviceBuffer>(buffer);
        }

        protected override ProxiedBuffer ResultFromContainer(ResourceContainer<IDeviceBuffer> container, Frame frame, ulong key, ulong frameId) => new ProxiedBuffer(container, frame);

        protected override ulong MakeKeyFromInput(BufferResourceDescriptor input) => input.Size;

        protected override ResourceContainer<IDeviceBuffer>? FindExistingResource(Dictionary<ulong, HashSet<ResourceContainer<IDeviceBuffer>>> items, Frame frame, ulong key, ulong frameId)
        {
            
            return items
                .Where(item => item.Key >= key &&  item.Key - key < MaxBufferReuseDelta)
                .SelectMany(c => c.Value)
                .FirstOrDefault(c => !c.Uses.Contains(frame));
        }
    }


    private readonly ImagePool _imagePool = new ImagePool();
    private readonly BufferPool _bufferPool = new BufferPool();
    
    private ulong _currentFrame = 0;
    
    public void Dispose()
    {
        _imagePool.Dispose();
        _bufferPool.Dispose();
    }

    public IDeviceImage CreateImage(ImageResourceDescriptor descriptor,Frame frame)
    {
        return _imagePool.Create(descriptor,frame,_currentFrame);
    }

    public IDeviceBuffer CreateBuffer(BufferResourceDescriptor descriptor, Frame frame)
    {
        return _bufferPool.Create(descriptor,frame,_currentFrame);
    }

    public void OnFrameStart(ulong newFrame)
    {
        _currentFrame = newFrame;
        var framesInFlight = renderer.GetNumFramesInFlight();
        _imagePool.CheckExpiredProxies(newFrame, framesInFlight);
        _bufferPool.CheckExpiredProxies(newFrame, framesInFlight);
    }
}