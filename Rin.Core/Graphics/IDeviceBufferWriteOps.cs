namespace Rin.Core.Graphics;

public interface IDeviceBufferWriteOps
{
    public unsafe void Write(void* src, ulong size, ulong offset = 0);
}