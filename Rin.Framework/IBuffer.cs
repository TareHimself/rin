namespace Rin.Framework;


/// <summary>
/// Represents a contiguous array of <see cref="T"/>
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IBuffer<T> : IReadOnlyBuffer<T>, IDisposable
    where T : unmanaged
{
    public void Zero();
    public unsafe T* GetData();
    public unsafe void Write(void* src, ulong size, ulong offset = 0);
    public unsafe void Write<TE>(TE* src, int numElements, ulong offset = 0) where TE : unmanaged;
    public void Write(IntPtr src, ulong size, ulong offset = 0);
    public void Write(ReadOnlySpan<T> data, ulong offset = 0);
    public Span<T> AsSpan();
}