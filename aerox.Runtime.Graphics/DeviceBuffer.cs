using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;
namespace aerox.Runtime.Graphics;

/// <summary>
///     GPU Buffer
/// </summary>
public class DeviceBuffer(VkBuffer inBuffer, ulong inSize, Allocator inAllocator, IntPtr inAllocation)
    : DeviceMemory(inAllocator,
        inAllocation)
{
    public VkBuffer Buffer = inBuffer;
    public ulong Size = inSize;

    public static implicit operator VkBuffer(DeviceBuffer from)
    {
        return from.Buffer;
    }

    protected override void OnDispose(bool isManual)
    {
        Allocator.FreeBuffer(this);
    }

    public ulong GetDeviceAddress()
    {
        var info = new VkBufferDeviceAddressInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_BUFFER_DEVICE_ADDRESS_INFO,
            buffer = Buffer
        };
        unsafe
        {
            return vkGetBufferDeviceAddress(SGraphicsModule.Get().GetDevice(), &info);
        }
    }
}