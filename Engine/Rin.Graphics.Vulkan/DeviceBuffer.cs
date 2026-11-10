using Rin.Core.Graphics;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace Rin.Graphics.Vulkan;

/// <summary>
///     GPU Buffer
/// </summary>
public class VulkanDeviceBuffer : IVulkanDeviceBuffer
{
    private readonly IntPtr _allocator;

    /// <summary>
    ///     GPU Buffer
    /// </summary>
    public VulkanDeviceBuffer(VkBuffer inBuffer, ulong inSize, IntPtr allocation, IntPtr allocator)
    {
        NativeBuffer = inBuffer;
        Size = inSize;
        Allocation = allocation;
        _allocator = allocator;
    }

    public ulong Offset => 0;
    public ulong Size { get; }
    public ResourceHandle Handle { get; set; } = ResourceHandle.InvalidBuffer;

    public VkBuffer NativeBuffer { get; set; }
    public IntPtr Allocation { get; }
    public BufferGraphState? GraphState { get; set; }

    public DeviceBufferView GetView(ulong offset, ulong size)
    {
        return new DeviceBufferView(Handle, offset, size);
    }

    public void Dispose()
    {
        VulkanGraphicsModule.Get().FreeBuffer(this);
        NativeBuffer = new VkBuffer();
    }

    public void WriteRaw(in IntPtr src, ulong size, ulong offset = 0)
    {
        Native.CopyToBuffer(_allocator, Allocation, src, size, offset);
    }

    public static implicit operator VkBuffer(VulkanDeviceBuffer from)
    {
        return from.NativeBuffer;
    }
}