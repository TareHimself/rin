using Rin.Engine.Core;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Core.Extensions;
using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Graphics;

public interface IDeviceBuffer : IGraphResource
{
    public ulong Offset { get; }
    public ulong Size { get; }
    public VkBuffer NativeBuffer { get; }
    public ulong GetAddress();
    public IDeviceBufferView GetView(ulong offset, ulong size);

    public unsafe void Write(void* src, ulong size, ulong offset = 0);

    public unsafe void Write<T>(IEnumerable<T> data, ulong offset = 0) where T : unmanaged
    {
        var asArray = data.ToArray();
        fixed (T* pData = asArray)
        {
            Write(pData, asArray.ByteSize(), offset);
        }
    }

    public void Write<T>(T src, ulong offset = 0) where T : unmanaged
    {
        unsafe
        {
            Write(&src, Core.Utils.ByteSizeOf<T>(), offset);
        }
    }

    public void Write<T>(NativeBuffer<T> src, ulong offset = 0) where T : unmanaged
    {
        unsafe
        {
            Write(src.GetData(), Core.Utils.ByteSizeOf<T>(src.GetElementsCount()), offset);
        }
    }
}