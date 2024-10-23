using System.Runtime.InteropServices;

namespace aerox.Runtime;

public class NativeBuffer<T>(int elements) : Disposable
    where T : unmanaged
{
    private readonly IntPtr _ptr = Marshal.AllocHGlobal((Marshal.SizeOf<T>()) * elements);

    public unsafe T* GetData()
    {
        return (T*)(_ptr);
    }
    
    public unsafe IntPtr GetPtr()
    {
        return _ptr;
    }

    public int GetElements() => elements;
    
    public int GetByteSize() => elements * Marshal.SizeOf<T>();

    protected override void OnDispose(bool isManual)
    {
        if (_ptr != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_ptr);
        }
    }
}