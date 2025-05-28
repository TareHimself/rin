using JetBrains.Annotations;
using Rin.Engine.Extensions;
using Rin.Engine.Graphics.Textures;
using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Graphics.FrameGraph;

public class ResourcePool(WindowRenderer renderer) : IResourcePool
{
    private readonly BufferPool _bufferPool = new();


    private readonly ImagePool _imagePool = new();

    private ulong _currentFrame;

    public void Dispose()
    {
        _imagePool.Dispose();
        _bufferPool.Dispose();
    }

    public IGraphImage CreateImage(ImageResourceDescriptor descriptor, Frame frame)
    {
        return _imagePool.Create(descriptor, frame, _currentFrame);
    }

    public IDeviceBuffer CreateBuffer(BufferResourceDescriptor descriptor, Frame frame)
    {
        return _bufferPool.Create(descriptor, frame, _currentFrame);
    }

    public void OnFrameStart(ulong newFrame)
    {
        _currentFrame = newFrame;
        var framesInFlight = renderer.GetNumFramesInFlight();
        _imagePool.CheckExpiredProxies(newFrame, framesInFlight);
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

    private sealed class ProxiedImage(ResourceContainer<IDeviceImage> container, ImageHandle handle, Frame frame)
        : IGraphImage
    {
        public ImageLayout Layout { get; set; } = ImageLayout.Undefined;
        public bool CreatedByGraph => true;

        public void Dispose()
        {
            container.Uses.Remove(frame);
        }

        public ImageFormat Format => container.Resource.Format;
        public Extent3D Extent => container.Resource.Extent;
        public VkImage NativeImage => container.Resource.NativeImage;
        public VkImageView NativeView { get; } = container.Resource.NativeView;
        public ImageHandle BindlessHandle => handle;
    }

    private class BindlessImage(ImageHandle handle, IDeviceImage image) : IDeviceImage
    {
        public ImageHandle BindlessHandle { get; } = handle;

        public void Dispose()
        {
            SGraphicsModule.Get().GetImageFactory().FreeHandles(BindlessHandle);
        }

        public ImageFormat Format => image.Format;
        public Extent3D Extent => image.Extent;
        public VkImage NativeImage => image.NativeImage;
        public VkImageView NativeView => image.NativeView;
    }

    private sealed class ImagePool : Pool<ProxiedImage, IDeviceImage, ImageResourceDescriptor, int>
    {
        protected override ResourceContainer<IDeviceImage> CreateNew(ImageResourceDescriptor input, Frame frame,
            int key, ulong frameId)
        {
            IDeviceImage image;
            if (input.Usage.HasFlag(ImageUsage.Sampled))
            {
                var (handle, img) = SGraphicsModule.Get().GetImageFactory().CreateTexture(input.Extent, input.Format,
                    usage: input.Usage, debugName: "Frame Graph Image");
                image = new BindlessImage(handle, img);
            }
            else
            {
                image = SGraphicsModule.Get().CreateDeviceImage(input.Extent, input.Format, input.Usage,
                    debugName: "Frame Graph Image");
            }

            return new ResourceContainer<IDeviceImage>(image);
        }

        protected override ProxiedImage ResultFromContainer(ResourceContainer<IDeviceImage> container, Frame frame,
            int key, ImageResourceDescriptor input, ulong frameId)
        {
            var imageHandle = container.Resource is BindlessImage asBindless
                ? asBindless.BindlessHandle
                : ImageHandle.InvalidImage;
            return new ProxiedImage(container, imageHandle, frame);
        }

        protected override int MakeKeyFromInput(ImageResourceDescriptor input)
        {
            return input.GetHashCode();
        }
    }


    private sealed class ProxiedBuffer : IDeviceBuffer
    {
        private readonly ResourceContainer<IDeviceBuffer> _container;
        private readonly Frame _frame;
        private readonly BufferResourceDescriptor _descriptor;

        public ProxiedBuffer(ResourceContainer<IDeviceBuffer> container, Frame frame, BufferResourceDescriptor descriptor)
        {
            _container = container;
            _frame = frame;
            _descriptor = descriptor;
        }

        public void Dispose()
        {
            _container.Uses.Remove(_frame);
        }

        public IDeviceBuffer Buffer => _container.Resource;
        public ulong Offset => _container.Resource.Offset;
        public ulong Size => _descriptor.Size;
        public VkBuffer NativeBuffer => _container.Resource.NativeBuffer;

        public ulong GetAddress()
        {
            return _container.Resource.GetAddress();
        }

        public IDeviceBufferView GetView(ulong offset, ulong size)
        {
            return new DeviceBufferView(this, offset, size);
        }

        public unsafe void Write(void* src, ulong size, ulong offset = 0)
        {
            _container.Resource.Write(src, size, offset);
        }
    }

    private class BufferPool : Pool<ProxiedBuffer, IDeviceBuffer, BufferResourceDescriptor, int>
    {
        [PublicAPI] public ulong MaxBufferReuseDelta = 1024;

        protected override ResourceContainer<IDeviceBuffer> CreateNew(BufferResourceDescriptor input, Frame frame,
            int key, ulong frameId)
        {
            var buffer = SGraphicsModule.Get().NewBuffer(input.Size, input.Usage, false, input.Mapped,"Frame Graph Storage Buffer");
            return new ResourceContainer<IDeviceBuffer>(buffer);
        }

        protected override ProxiedBuffer ResultFromContainer(ResourceContainer<IDeviceBuffer> container, Frame frame,
            int key, BufferResourceDescriptor input, ulong frameId)
        {
            return new ProxiedBuffer(container, frame, input);
        }

        protected override int MakeKeyFromInput(BufferResourceDescriptor input)
        {
            return input.GetHashCode();
        }

        protected override ResourceContainer<IDeviceBuffer>? FindExistingResource(
            Dictionary<int, HashSet<ResourceContainer<IDeviceBuffer>>> items, Frame frame, int key, ulong frameId)
        {
            return items
                .Where(item => item.Key == key)
                .SelectMany(c => c.Value)
                .FirstOrDefault(c => c.Uses.Empty());
// #if DEBUG
//             return items
//                 .Where(item => item.Key == key)
//                 .SelectMany(c => c.Value)
//                 .FirstOrDefault(c => c.Uses.Empty());
// #else
//             return items
//                 .Where(item => item.Key >= key && item.Key - key < MaxBufferReuseDelta)
//                 .SelectMany(c => c.Value)
//                 .FirstOrDefault(c => c.Uses.Empty());
// #endif
        }
    }
}