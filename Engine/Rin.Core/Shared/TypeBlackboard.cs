using System.Runtime.InteropServices;

namespace Rin.Core.Shared;

public sealed class TypeBlackboard : IDisposable
{
    private readonly Dictionary<Type, IntPtr> _data = [];

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    public ref T Get<T>() where T : unmanaged
    {
        unsafe
        {
            var type = typeof(T);
            if (!_data.ContainsKey(type)) _data.Add(type, (IntPtr)NativeMemory.Alloc((nuint)sizeof(T)));
            var ptr = _data[type].ToPointer();
            return ref *(T*)ptr;
        }
    }

    private unsafe void ReleaseUnmanagedResources()
    {
        foreach (var ptr in _data.Values) NativeMemory.Free(ptr.ToPointer());
    }

    ~TypeBlackboard()
    {
        ReleaseUnmanagedResources();
    }
}