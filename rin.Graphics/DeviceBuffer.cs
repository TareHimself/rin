using System.Runtime.InteropServices;
using rin.Core;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;
namespace rin.Graphics;

/// <summary>
///     GPU Buffer
/// </summary>
public class DeviceBuffer : DeviceMemory
{
    public VkBuffer Buffer;
    public readonly ulong Size;
    private ulong? _address;

    public class View : MultiDisposable
    {
        public readonly DeviceBuffer Buffer;
        public readonly ulong Offset;
        public readonly ulong Size;

        public View(DeviceBuffer buffer, ulong offset, ulong size)
        {
            Buffer = buffer;
            Offset = offset;
            Size = size;
            buffer.Reserve();
        }

        protected override void OnDispose(bool isManual)
        {
            Buffer.Dispose();
        }
        
        public unsafe void Write(void* src, ulong size, ulong offset = 0) => Buffer.Write(src,size,Offset + offset);

        public void Write<T>(T[] data, ulong offset = 0) => Buffer.Write(data,Offset + offset);

        public void Write<T>(T src, ulong offset = 0) => Buffer.Write(src,Offset + offset);
    
        public void Write<T>(NativeBuffer<T> src, ulong offset = 0) where T : unmanaged => Buffer.Write(src,Offset + offset);

        public static implicit operator VkBuffer(View view) => view.Buffer.Buffer;

        public ulong GetAddress() => Buffer.GetAddress() + Offset;
        
        public DeviceBuffer.View GetView(ulong offset, ulong size) => new View(Buffer,Offset + offset, size);
    }
    /// <summary>
    ///     GPU Buffer
    /// </summary>
    public DeviceBuffer(VkBuffer inBuffer, ulong inSize, Allocator allocator, IntPtr allocation) : base(allocator,
        allocation)
    {
        Buffer = inBuffer;
        Size = inSize;
    }


    public static implicit operator VkBuffer(DeviceBuffer from)
    {
        return from.Buffer;
    }

    protected override void OnDispose(bool isManual)
    {
        Allocator.FreeBuffer(this);
    }

    public ulong GetAddress()
    {
        if (_address.HasValue) return _address.Value;
        
        var info = new VkBufferDeviceAddressInfo()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_BUFFER_DEVICE_ADDRESS_INFO,
            buffer = Buffer
        };
        unsafe
        {
            _address = vkGetBufferDeviceAddress(SGraphicsModule.Get().GetDevice(), &info);
        }

        return _address.Value;
    }
    
    public unsafe void Write(void* src, ulong size, ulong offset = 0)
    {
        NativeMethods.CopyToBuffer(Allocator, Allocation.ToPointer(), src, size, offset);
    }

    public void Write<T>(T[] data, ulong offset = 0)
    {
        unsafe
        {
            fixed (T* pData = data)
            {
                Write(pData, (ulong)(data.Length * Marshal.SizeOf<T>()), offset);
            }
        }
    }

    public void Write<T>(T src, ulong offset = 0)
    {
        unsafe
        {
            Write(&src, (ulong)Marshal.SizeOf<T>(), offset);
        }
    }
    
    public void Write<T>(NativeBuffer<T> src, ulong offset = 0) where T : unmanaged
    {
        unsafe
        {
            Write(src.GetData(), (ulong)(Marshal.SizeOf<T>() * src.GetElements()), offset);
        }
    }

    public static implicit operator View(DeviceBuffer buff) => new View(buff, 0, buff.Size);

    public DeviceBuffer.View GetView(ulong offset, ulong size) => new View(this, offset, size);
}