using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace Rin.Framework.Graphics.Vulkan;

/// <summary>
///     GPU Buffer
/// </summary>
public class VulkanDeviceBuffer : IVulkanDeviceBuffer
{
    private nuint? _address;

    private IntPtr _allocator;
    /// <summary>
    ///     GPU Buffer
    /// </summary>
    public VulkanDeviceBuffer(VkBuffer inBuffer, ulong inSize,  IntPtr allocation,IntPtr allocator)
    {
        NativeBuffer = inBuffer;
        Size = inSize;
        Allocation = allocation;
        _allocator = allocator;
    }
    
    public IDeviceBuffer Buffer => this;
    public ulong Offset => 0;
    public ulong Size { get; }

    public VkBuffer NativeBuffer { get; set; }
    public IntPtr Allocation { get; }

    public ulong GetAddress()
    {
        if (_address.HasValue) return _address.Value;

        var info = new VkBufferDeviceAddressInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_BUFFER_DEVICE_ADDRESS_INFO,
            buffer = NativeBuffer
        };
        unsafe
        {
            _address = (nuint)vkGetBufferDeviceAddress(VulkanGraphicsModule.Get().GetDevice(), &info);
        }

        return _address.Value;
    }

    public DeviceBufferView GetView(ulong offset, ulong size)
    {
        return new DeviceBufferView(this, offset, size);
    }
    
    public void Dispose()
    {
        VulkanGraphicsModule.Get().FreeBuffer(this);
        NativeBuffer = new VkBuffer();
    }

    public void WriteRaw(in IntPtr src, ulong size, ulong offset = 0)
    {
        Native.Vulkan.CopyToBuffer(_allocator,Allocation, src, size, offset);
    }

    public static implicit operator VkBuffer(VulkanDeviceBuffer from)
    {
        return from.NativeBuffer;
    }
}