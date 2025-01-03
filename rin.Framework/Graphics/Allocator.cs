﻿using System.Runtime.CompilerServices;
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
    private readonly IntPtr _allocator;
    private readonly SGraphicsModule _module;

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
            NativeMethods.AllocateBuffer(&buffer, &allocation, size, _allocator, sequentialWrite ? 1 : 0,
                preferHost ? 1 : 0,
                (int)usageFlags, (int)propertyFlags, mapped ? 1 : 0, debugName);

            return new DeviceBuffer(buffer, size, this, (IntPtr)allocation);
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
            NativeMethods.AllocateImage(&image, &allocation,&imageCreateInfo, _allocator, debugName);

            return new DeviceImage(image, new VkImageView(), imageCreateInfo.extent, SGraphicsModule.VkFormatToImageFormat(imageCreateInfo.format), this,
                (IntPtr)allocation);
        }
    }

    /// <summary>
    ///     Free's a <see cref="DeviceBuffer" />
    /// </summary>
    public void FreeBuffer(DeviceBuffer buffer)
    {
        NativeMethods.FreeBuffer(buffer.NativeBuffer, buffer.Allocation, _allocator);
    }

    /// <summary>
    ///     Free's a <see cref="DeviceImage" />
    /// </summary>
    public void FreeImage(DeviceImage image)
    {
        unsafe
        {
            vkDestroyImageView(_module.GetDevice(), image.NativeView, null);
            NativeMethods.FreeImage(image.NativeImage, image.Allocation, _allocator);
        }
    }
}