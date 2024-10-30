using System.Runtime.InteropServices;

namespace rin.Graphics;

public class DeviceBufferSlice
{
    public DeviceBufferSlice(DeviceBuffer buffer,ulong offset = 0)
    {
        Buffer = buffer;
        Offset = offset;
    }

    public DeviceBuffer Buffer { get; private set; }
    public ulong Offset { get; private set; }
    
    public unsafe void Write(void* src, ulong size)
    {
        Buffer.Write(src,size,Offset);
    }

    public void Write<T>(T[] data)
    {
        unsafe
        {
            fixed (T* pData = data)
            {
                Write(pData, (ulong)(data.Length * Marshal.SizeOf<T>()));
            }
        }
    }

    public void Write<T>(T src, ulong offset = 0)
    {
        unsafe
        {
            Write(&src, (ulong)Marshal.SizeOf<T>());
        }
    }
}