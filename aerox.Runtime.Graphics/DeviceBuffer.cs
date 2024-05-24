using TerraFX.Interop.Vulkan;

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

    public static implicit operator VkBuffer(DeviceBuffer from) => from.Buffer;

    protected override void OnDispose(bool isManual)
    {
        Allocator.FreeBuffer(this);
    }
}