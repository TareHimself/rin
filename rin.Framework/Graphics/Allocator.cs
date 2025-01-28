using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using rin.Framework.Core;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace rin.Framework.Graphics;

/// <summary>
///     Allocates GPU memory
/// </summary>
public partial class Allocator : Disposable
{
    class WeakReferenceComparer<T> : IEqualityComparer<WeakReference<T>> where T : class
    {
        public bool Equals(WeakReference<T>? x, WeakReference<T>? y)
        {
            T? a = null;
            T? b = null;
            x?.TryGetTarget(out a);
            y?.TryGetTarget(out b);
            return a == b;
        }

        public int GetHashCode(WeakReference<T> obj)
        {
            obj.TryGetTarget(out var a);
            return a?.GetHashCode() ?? 0;
        }
    }
 
    private readonly IntPtr _allocator;
    private readonly SGraphicsModule _module;

    private HashSet<IDeviceBuffer> _allocatedBuffers = [];

    private HashSet<IDeviceImage> _allocatedImages = [];

    public Allocator(SGraphicsModule module)
    {
        unsafe
        {
            _module = module;
            _allocator = NativeMethods.CreateAllocator(module.GetInstance(), module.GetDevice(),
                module.GetPhysicalDevice());
        }
    }

    public static implicit operator IntPtr(Allocator allocator)
    {
        return allocator._allocator;
    }


    protected override void OnDispose(bool isManual)
    {
        NativeMethods.DestroyAllocator(_allocator);
    }

    /// <summary>
    ///     Allocates a <see cref="DeviceBuffer" />
    /// </summary>
    public IDeviceBuffer NewBuffer(ulong size, VkBufferUsageFlags usageFlags, VkMemoryPropertyFlags propertyFlags,
        bool sequentialWrite = true, bool preferHost = false, bool mapped = false, string debugName = "Buffer")
    {
        unsafe
        {
            VkBuffer buffer = new();
            void* allocation;
            NativeMethods.AllocateBuffer(&buffer, &allocation, (ulong)size, _allocator, sequentialWrite ? 1 : 0,
                preferHost ? 1 : 0,
                (int)usageFlags, (int)propertyFlags, mapped ? 1 : 0, debugName);
            var result = new DeviceBuffer(buffer, size, this, (IntPtr)allocation, debugName);
            _allocatedBuffers.Add(result);
            return result;
        }
    }


    /// <summary>
    ///     Allocates a <see cref="DeviceImage" />
    /// </summary>
    public IDeviceImage NewDeviceImage(VkImageCreateInfo imageCreateInfo, string debugName = "Image")
    {
        unsafe
        {
            VkImage image = new();
            void* allocation;
            NativeMethods.AllocateImage(&image, &allocation, &imageCreateInfo, _allocator, debugName);
            var result = new DeviceImage(image, new VkImageView(), imageCreateInfo.extent,
                imageCreateInfo.format.FromVk(), this,
                (IntPtr)allocation, debugName);
            _allocatedImages.Add(result);
            return result;
        }
    }

    /// <summary>
    ///     Free's a <see cref="DeviceBuffer" />
    /// </summary>
    public void FreeBuffer(DeviceBuffer buffer)
    {
        _allocatedBuffers.Remove(buffer);
        NativeMethods.FreeBuffer(buffer.NativeBuffer, buffer.Allocation, _allocator);
    }

    /// <summary>
    ///     Free's a <see cref="DeviceImage" />
    /// </summary>
    public void FreeImage(DeviceImage image)
    {
        unsafe
        {
            _allocatedImages.Remove(image);
            vkDestroyImageView(_module.GetDevice(), image.NativeView, null);
            NativeMethods.FreeImage(image.NativeImage, image.Allocation, _allocator);
        }
    }
}