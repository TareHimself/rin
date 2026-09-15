namespace Rin.Core;

public class Pointer<T> : IDisposable where T : unmanaged
{
    private readonly IntPtr _pointer;

    public Pointer()
    {
        unsafe
        {
            _pointer = Native.memoryAllocate((ulong)sizeof(T));
        }
    }

    public void Dispose()
    {
        Native.memoryFree(_pointer);
    }
}