namespace Rin.Framework;


/// <summary>
/// Represents a contiguous array of <see cref="T"/>
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IReadOnlyBuffer<T>
    where T : unmanaged
{
    public int ElementCount { get; }
    public ulong ByteSize { get; }
    public T GetElement(int index);
    public IntPtr GetPtr();
    public ReadOnlySpan<T> AsReadOnlySpan();
}