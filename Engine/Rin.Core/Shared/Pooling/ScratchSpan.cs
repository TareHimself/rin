using System.Buffers;

namespace Rin.Core.Shared.Pooling;

/// <summary>
///     A zeroed scratch span of an exact length: the caller's stack buffer when one is given, otherwise a pooled array
///     that goes back to <see cref="ArrayPool{T}.Shared" /> on <see cref="Dispose" />.
/// </summary>
/// <remarks>
///     Stack memory has to be allocated by the caller, so pass it conditionally:
///     <code>
///     using var scratch = new ScratchSpan&lt;T&gt;(count, count &lt;= 8 ? stackalloc T[count] : default);
///     </code>
/// </remarks>
public ref struct ScratchSpan<T> where T : unmanaged
{
    private T[]? _rented;

    public ScratchSpan(int length, Span<T> stack)
    {
        if (length <= stack.Length)
        {
            Span = stack[..length];
            Span.Clear();
            return;
        }

        _rented = ArrayPool<T>.Shared.Rent(length);
        Span = _rented.AsSpan(0, length);
        Span.Clear();
    }

    public Span<T> Span { get; }

    public void Dispose()
    {
        if (_rented is null) return;
        ArrayPool<T>.Shared.Return(_rented);
        _rented = null;
    }
}
