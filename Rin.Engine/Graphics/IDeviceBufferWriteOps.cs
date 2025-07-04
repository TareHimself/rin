namespace Rin.Engine.Graphics;

public interface IDeviceBufferWriteOps
{
    public unsafe void Write(void* src, ulong size, ulong offset = 0);
    public void WriteArray<T>(IEnumerable<T> data, ulong offset = 0) where T : unmanaged;
    public void Write(in IntPtr src, ulong size, ulong offset = 0);
    public void WriteStruct<T>(T src, ulong offset = 0) where T : unmanaged;
    public void WriteBuffer<T>(Buffer<T> src, ulong offset = 0) where T : unmanaged;
}