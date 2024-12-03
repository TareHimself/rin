using System.Runtime.InteropServices;
using rin.Framework.Core;
using rin.Framework.Graphics.FrameGraph;
using TerraFX.Interop.Vulkan;

namespace rin.Framework.Graphics;

public interface IDeviceBuffer : IReservable,IGraphResource
{
    public ulong Offset { get; }
    public ulong Size { get; }
    public VkBuffer NativeBuffer { get ;}
    public ulong GetAddress();
    public IDeviceBuffer GetView(ulong offset,ulong size);

    public unsafe void Write(void* src, ulong size, ulong offset = 0);

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
}