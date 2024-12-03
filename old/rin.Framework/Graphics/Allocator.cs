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
    ///     Allocates a <see cref="DeviceBuffer" /> for transfers/staging
    /// </summary>
    public IDeviceBuffer NewTransferBuffer(ulong size, bool sequentialWrite = true,
        string debugName = "Transfer Buffer")
    {
        return NewBuffer(size, VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_SRC_BIT,
            VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT |
            VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_COHERENT_BIT
            , sequentialWrite, true, true, debugName);
    }

    /// <summary>
    ///     Allocates a <see cref="DeviceBuffer" /> for transfers/staging
    /// </summary>
    public IDeviceBuffer NewStorageBuffer<T>(bool sequentialWrite = true, string debugName = "storageBuffer")
    {
        return NewStorageBuffer((ulong)Marshal.SizeOf<T>(), sequentialWrite, debugName);
    }
    
    /// <summary>
    ///     Allocates a <see cref="DeviceBuffer" /> for shader uniforms
    /// </summary>
    public IDeviceBuffer NewStorageBuffer(ulong size, bool sequentialWrite = true,
        string debugName = "Storage Buffer")
    {
        return NewBuffer(size, VkBufferUsageFlags.VK_BUFFER_USAGE_STORAGE_BUFFER_BIT | VkBufferUsageFlags.VK_BUFFER_USAGE_SHADER_DEVICE_ADDRESS_BIT,
            VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT, sequentialWrite, false, true, debugName);
    }

    /// <summary>
    ///     Allocates a <see cref="DeviceBuffer" /> for shader uniforms
    /// </summary>
    public IDeviceBuffer NewUniformBuffer(ulong size, bool sequentialWrite = true,
        string debugName = "Uniform Buffer")
    {
        return NewBuffer(size, VkBufferUsageFlags.VK_BUFFER_USAGE_UNIFORM_BUFFER_BIT,
            VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT, sequentialWrite, false, true, debugName);
    }

    /// <summary>
    ///     Allocates a <see cref="DeviceBuffer" /> for shader uniforms
    /// </summary>
    public IDeviceBuffer NewUniformBuffer<T>(bool sequentialWrite = true, string debugName = "uniformBuffer")
    {
        return NewUniformBuffer((ulong)Marshal.SizeOf<T>(), sequentialWrite, debugName);
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