using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace rin.Framework.Core.Extensions;

public static class StructExtensions
{
    public static Span<TElement> InlineArrayAsSpan<TBuffer, TElement>(this ref TBuffer buffer, int size)
        where TBuffer : struct
    {
        return MemoryMarshal.CreateSpan(ref Unsafe.As<TBuffer, TElement>(ref buffer), size);
    }

    public static ReadOnlySpan<TElement> InlineArrayAsReadOnlySpan<TBuffer, TElement>(this TBuffer buffer, int size)
        where TBuffer : struct
    {
        return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TBuffer, TElement>(ref Unsafe.AsRef(in buffer)), size);
    }
}