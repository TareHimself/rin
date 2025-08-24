namespace Rin.Framework;

public class Pointer<T> : IDisposable where T : unmanaged
{
    private readonly IntPtr _pointer;

    public Pointer()
    {
        unsafe
        {
            _pointer = Native.Memory.Allocate((ulong)sizeof(T));
        }
    }

    public void Dispose()
    {
        Native.Memory.Free(_pointer);
    }
}