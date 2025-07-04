using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace Rin.Engine.Graphics;

/// <summary>
///     GPU Buffer
/// </summary>
public class DeviceBuffer : DeviceMemory, IDeviceBuffer
{
    private nuint? _address;
    
    /// <summary>
    ///     GPU Buffer
    /// </summary>
    public DeviceBuffer(VkBuffer inBuffer, ulong inSize, Allocator allocator, IntPtr allocation, string name) : base(
        allocator,
        allocation)
    {
        NativeBuffer = inBuffer;
        Size = inSize;
        Name = name;
    }

    public string Name { get; private set; }

    public IDeviceBuffer Buffer => this;
    public ulong Offset => 0;
    public ulong Size { get; }

    public VkBuffer NativeBuffer { get; set; }

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
            _address = (nuint)vkGetBufferDeviceAddress(SGraphicsModule.Get().GetDevice(), &info);
        }

        return _address.Value;
    }

    public DeviceBufferView GetView(ulong offset, ulong size)
    {
        return new DeviceBufferView(this, offset, size);
    }

    public unsafe void Write(void* src, ulong size, ulong offset = 0)
    {
        Native.Vulkan.CopyToBuffer(Allocator, Allocation.ToPointer(), src, size, offset);
    }


    public static implicit operator VkBuffer(DeviceBuffer from)
    {
        return from.NativeBuffer;
    }

    public override void Dispose()
    {
        Allocator.FreeBuffer(this);
        NativeBuffer = new VkBuffer();
    }
    
    public void WriteArray<T>(IEnumerable<T> data, ulong offset = 0) where T : unmanaged
    {
        unsafe
        {
            var asArray = data.ToArray();
            fixed (T* pData = asArray)
            {
                Write(pData, Utils.ByteSizeOf<T>(asArray.Length), offset);
            }
        }
    }
    
    public void Write(in IntPtr src, ulong size, ulong offset = 0)
    {
        unsafe
        {
            Write(src.ToPointer(),size, offset);
        }
    }

    public void WriteStruct<T>(T src, ulong offset = 0) where T : unmanaged
    {
        unsafe
        {
            Write(&src, Utils.ByteSizeOf<T>(), offset);
        }
    }

    public void WriteBuffer<T>(Buffer<T> src, ulong offset = 0) where T : unmanaged
    {
        unsafe
        {
            Write(src.GetData(), Utils.ByteSizeOf<T>(src.GetElementsCount()), offset);
        }
    }
}