using System.Runtime.InteropServices;
using rin.Framework.Core;
using rin.Framework.Graphics.FrameGraph;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;
namespace rin.Framework.Graphics;

public interface IDeviceBuffer : IReservable,IGraphResource
{
    public int Offset { get; }
    public int Size { get; }
    public VkBuffer NativeBuffer { get ;}
    public ulong GetAddress();
    public IDeviceBuffer GetView(int offset, int size);

    public unsafe void Write(void* src, int size, int offset = 0);

    public unsafe void Write<T>(T[] data,int offset = 0)
    {
        unsafe
        {
            fixed (T* pData = data)
            {
                Write(pData, data.Length * Marshal.SizeOf<T>(), offset);
            }
        }
    }

    public void Write<T>(T src, int offset = 0) where T : unmanaged
    {
        unsafe
        {
            Write(&src, Marshal.SizeOf<T>(), offset);
        }
    }
    
    public void Write<T>(NativeBuffer<T> src, int offset = 0) where T : unmanaged
    {
        unsafe
        {
            Write(src.GetData(), Marshal.SizeOf<T>() * src.GetElements(), offset);
        }
    }
    
}