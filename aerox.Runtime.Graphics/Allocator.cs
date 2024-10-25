using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace aerox.Runtime.Graphics;

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
            _allocator = NativeCreateAllocator(module.GetInstance().Value, module.GetDevice().Value,
                module.GetPhysicalDevice().Value);
        }
    }

    [LibraryImport(Dlls.AeroxGraphicsNative, EntryPoint = "graphicsAllocatorCreate")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe partial IntPtr NativeCreateAllocator(void* instance, void* device,
        void* physicalDevice);

    [LibraryImport(Dlls.AeroxGraphicsNative, EntryPoint = "graphicsAllocatorDestroy")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void NativeDestroyAllocator(IntPtr allocator);


    [LibraryImport(Dlls.AeroxGraphicsNative, EntryPoint = "graphicsAllocatorNewBuffer")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe partial void NativeAllocateBuffer(ulong* buffer, void** allocation, ulong size,
        IntPtr allocator,
        int sequentialWrite, int preferHost, int usageFlags, int memoryPropertyFlags,
        int mapped, [MarshalAs(UnmanagedType.BStr)] string debugName);

    [LibraryImport(Dlls.AeroxGraphicsNative, EntryPoint = "graphicsAllocatorNewImage")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe partial void NativeAllocateImage(ulong* image, void** allocation,
        void* createInfo, IntPtr allocator, [MarshalAs(UnmanagedType.BStr)] string debugName);

    [LibraryImport(Dlls.AeroxGraphicsNative, EntryPoint = "graphicsAllocatorFreeBuffer")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void NativeFreeBuffer(ulong buffer, IntPtr allocation, IntPtr allocator);

    [LibraryImport(Dlls.AeroxGraphicsNative, EntryPoint = "graphicsAllocatorFreeImage")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static partial void NativeFreeImage(ulong image, IntPtr allocation, IntPtr allocator);

    public static implicit operator IntPtr(Allocator allocator)
    {
        return allocator._allocator;
    }


    protected override void OnDispose(bool isManual)
    {
        NativeDestroyAllocator(_allocator);
    }

    /// <summary>
    ///     Allocates a <see cref="DeviceBuffer" />
    /// </summary>
    public DeviceBuffer NewBuffer(ulong size, VkBufferUsageFlags usageFlags, VkMemoryPropertyFlags propertyFlags,
        bool sequentialWrite = true, bool preferHost = false, bool mapped = false, string debugName = "Buffer")
    {
        unsafe
        {
            VkBuffer buffer = new();
            void* allocation;
            NativeAllocateBuffer(&buffer.Value, &allocation, size, _allocator, sequentialWrite ? 1 : 0,
                preferHost ? 1 : 0,
                (int)usageFlags, (int)propertyFlags, mapped ? 1 : 0, debugName);

            return new DeviceBuffer(buffer, size, this, (IntPtr)allocation);
        }
    }

    /// <summary>
    ///     Allocates a <see cref="DeviceBuffer" /> for transfers/staging
    /// </summary>
    public DeviceBuffer NewTransferBuffer(ulong size, bool sequentialWrite = true,
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
    public DeviceBuffer NewStorageBuffer<T>(bool sequentialWrite = true, string debugName = "storageBuffer")
    {
        return NewStorageBuffer((ulong)Marshal.SizeOf<T>(), sequentialWrite, debugName);
    }
    
    /// <summary>
    ///     Allocates a <see cref="DeviceBuffer" /> for shader uniforms
    /// </summary>
    public DeviceBuffer NewStorageBuffer(ulong size, bool sequentialWrite = true,
        string debugName = "Storage Buffer")
    {
        return NewBuffer(size, VkBufferUsageFlags.VK_BUFFER_USAGE_STORAGE_BUFFER_BIT | VkBufferUsageFlags.VK_BUFFER_USAGE_SHADER_DEVICE_ADDRESS_BIT,
            VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT, sequentialWrite, false, true, debugName);
    }

    /// <summary>
    ///     Allocates a <see cref="DeviceBuffer" /> for shader uniforms
    /// </summary>
    public DeviceBuffer NewUniformBuffer(ulong size, bool sequentialWrite = true,
        string debugName = "Uniform Buffer")
    {
        return NewBuffer(size, VkBufferUsageFlags.VK_BUFFER_USAGE_UNIFORM_BUFFER_BIT,
            VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT, sequentialWrite, false, true, debugName);
    }

    /// <summary>
    ///     Allocates a <see cref="DeviceBuffer" /> for shader uniforms
    /// </summary>
    public DeviceBuffer NewUniformBuffer<T>(bool sequentialWrite = true, string debugName = "uniformBuffer")
    {
        return NewUniformBuffer((ulong)Marshal.SizeOf<T>(), sequentialWrite, debugName);
    }

    /// <summary>
    ///     Allocates a <see cref="DeviceImage" />
    /// </summary>
    public DeviceImage NewDeviceImage(VkImageCreateInfo imageCreateInfo, string debugName = "Image")
    {
        unsafe
        {
            VkImage image = new();
            void* allocation;
            NativeAllocateImage(&image.Value, &allocation, &imageCreateInfo, _allocator, debugName);

            return new DeviceImage(image, new VkImageView(), imageCreateInfo.extent, SGraphicsModule.VkFormatToImageFormat(imageCreateInfo.format), this,
                (IntPtr)allocation);
        }
    }

    /// <summary>
    ///     Free's a <see cref="DeviceBuffer" />
    /// </summary>
    public void FreeBuffer(DeviceBuffer buffer)
    {
        NativeFreeBuffer(buffer.Buffer, buffer.Allocation, _allocator);
    }

    /// <summary>
    ///     Free's a <see cref="DeviceImage" />
    /// </summary>
    public void FreeImage(DeviceImage image)
    {
        unsafe
        {
            vkDestroyImageView(_module.GetDevice(), image.View, null);
        }

        NativeFreeImage(image.Image, image.Allocation, _allocator);
    }
}