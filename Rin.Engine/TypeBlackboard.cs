namespace Rin.Engine;

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
            if (!_data.ContainsKey(type)) _data.Add(type, Native.Memory.Allocate((ulong)sizeof(T)));
            var ptr = _data[type].ToPointer();
            return ref *(T*)ptr;
        }
    }

    private void ReleaseUnmanagedResources()
    {
        foreach (var ptr in _data.Values) Native.Memory.Free(ptr);
    }

    ~TypeBlackboard()
    {
        ReleaseUnmanagedResources();
    }
}