using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Graphics;

public interface IDeviceBufferView
{
    public IDeviceBuffer Buffer { get; }

    public ulong Offset { get; }
    public ulong Size { get; }
    public VkBuffer NativeBuffer { get; }

    public ulong GetAddress()
    {
        return Buffer.GetAddress() + Offset;
    }

    public IDeviceBufferView GetView(ulong offset, ulong size);

    public unsafe void Write(void* src, ulong size, ulong offset = 0)
    {
        Buffer.Write(src, size, Offset + offset);
    }

    public void Write<T>(IEnumerable<T> data, ulong offset = 0) where T : unmanaged
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

    public void Write<T>(T src, ulong offset = 0) where T : unmanaged
    {
        unsafe
        {
            Write(&src, Utils.ByteSizeOf<T>(), offset);
        }
    }

    public void Write<T>(Buffer<T> src, ulong offset = 0) where T : unmanaged
    {
        unsafe
        {
            Write(src.GetData(), Utils.ByteSizeOf<T>(src.GetElementsCount()), offset);
        }
    }
}