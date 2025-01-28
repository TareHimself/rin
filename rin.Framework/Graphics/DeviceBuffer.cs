using System.Runtime.InteropServices;
using rin.Framework.Core;
using rin.Framework.Graphics.FrameGraph;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;
namespace rin.Framework.Graphics;

/// <summary>
///     GPU Buffer
/// </summary>
public class DeviceBuffer : DeviceMemory, IDeviceBuffer
{
    private ulong? _address;
    
    public string Name { get; private set; }
    /// <summary>
    ///     GPU Buffer
    /// </summary>
    public DeviceBuffer(VkBuffer inBuffer, ulong inSize, Allocator allocator, IntPtr allocation, string name) : base(allocator,
        allocation)
    {
        NativeBuffer = inBuffer;
        Size = inSize;
        Name = name;
    }


    public static implicit operator VkBuffer(DeviceBuffer from)
    {
        return from.NativeBuffer;
    }

    protected override void OnDispose(bool isManual)
    {
        Allocator.FreeBuffer(this);
    }

    public ulong Offset => 0;
    public ulong Size { get; }

    public VkBuffer NativeBuffer { get; }

    public ulong GetAddress()
    {
        if (_address.HasValue) return _address.Value;
        
        var info = new VkBufferDeviceAddressInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_BUFFER_DEVICE_ADDRESS_INFO,
            buffer = NativeBuffer
        };
        unsafe
        {
            _address = vkGetBufferDeviceAddress(SGraphicsModule.Get().GetDevice(), &info);
        }

        return _address.Value;
    }

    public IDeviceBufferView GetView(ulong offset, ulong size)
    {
        return new DeviceBufferView(this, offset, size);
    }

    public unsafe void Write(void* src, ulong size, ulong offset = 0)
    {
        NativeMethods.CopyToBuffer(Allocator, Allocation.ToPointer(), src,size,offset);
    }
}