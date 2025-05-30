﻿using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace Rin.Engine.Graphics;

/// <summary>
///     Allocates GPU memory
/// </summary>
public class Allocator(SGraphicsModule module) : Disposable
{
    private readonly IntPtr _allocator = Native.Vulkan.CreateAllocator(module.GetInstance(), module.GetDevice(),
        module.GetPhysicalDevice());

    public static implicit operator IntPtr(Allocator allocator)
    {
        return allocator._allocator;
    }


    protected override void OnDispose(bool isManual)
    {
        Native.Vulkan.DestroyAllocator(_allocator);
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
            Native.Vulkan.AllocateBuffer(&buffer, &allocation, size, _allocator, sequentialWrite ? 1 : 0,
                preferHost ? 1 : 0,
                (int)usageFlags, (int)propertyFlags, mapped ? 1 : 0, debugName);
            var result = new DeviceBuffer(buffer, size, this, (IntPtr)allocation, debugName);
#if DEBUG
            lock (_buffers)
            {
                _buffers.Add(new WeakReference<IDeviceBuffer>(result));
            }
#endif
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
            Native.Vulkan.AllocateImage(&image, &allocation, &imageCreateInfo, _allocator, debugName);
            var result = new DeviceImage(image, new VkImageView(), new Extent3D
                {
                    Width = imageCreateInfo.extent.width,
                    Height = imageCreateInfo.extent.height,
                    Dimensions = imageCreateInfo.extent.depth
                },
                imageCreateInfo.format.FromVk(), this,
                (IntPtr)allocation, debugName);
#if DEBUG
            lock (_images)
            {
                _images.Add(new WeakReference<IDeviceImage>(result));
            }
#endif
            return result;
        }
    }

    /// <summary>
    ///     Free's a <see cref="DeviceBuffer" />
    /// </summary>
    public void FreeBuffer(DeviceBuffer buffer)
    {
#if DEBUG
        lock (_buffers)
        {
            _buffers.RemoveWhere(c =>
            {
                c.TryGetTarget(out var target);
                return buffer == target;
            });
        }
#endif
        Native.Vulkan.FreeBuffer(buffer.NativeBuffer, buffer.Allocation, _allocator);
    }

    /// <summary>
    ///     Free's a <see cref="DeviceImage" />
    /// </summary>
    public void FreeImage(DeviceImage image)
    {
        unsafe
        {
#if DEBUG
            lock (_images)
            {
                _images.RemoveWhere(c =>
                {
                    c.TryGetTarget(out var target);
                    return image == target;
                });
            }
#endif
            vkDestroyImageView(module.GetDevice(), image.NativeView, null);
            Native.Vulkan.FreeImage(image.NativeImage, image.Allocation, _allocator);
        }
    }

#if DEBUG
    private readonly HashSet<WeakReference<IDeviceBuffer>> _buffers =
        new(new WeakReferenceEqualityComparer<IDeviceBuffer>());

    private readonly HashSet<WeakReference<IDeviceImage>> _images =
        new(new WeakReferenceEqualityComparer<IDeviceImage>());
#endif
}