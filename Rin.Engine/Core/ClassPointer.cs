using System.Runtime.InteropServices;

namespace Rin.Engine.Core;

public readonly struct ClassPointer<T>  : IDisposable where T : class
{
    private readonly IntPtr _handle = IntPtr.Zero;

    public ClassPointer(T value,GCHandleType handleType)
    {
        _handle = GCHandle.ToIntPtr(GCHandle.Alloc(value,handleType));
    }
    
    public ClassPointer(IntPtr handle)
    {
        _handle = handle;
    }

    public void Dispose()
    {
        if (_handle == IntPtr.Zero) return;
        var handle = GCHandle.FromIntPtr(_handle);
        handle.Free();
    }

    public static implicit operator T(ClassPointer<T> ptr)
    {
        if (ptr._handle == IntPtr.Zero) throw new NullReferenceException();
        var gcHandle = GCHandle.FromIntPtr(ptr._handle);
        return (T?)gcHandle.Target ?? throw new InvalidOperationException();
    }
}