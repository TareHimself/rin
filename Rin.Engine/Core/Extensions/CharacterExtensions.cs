using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Rin.Engine.Core.Extensions;

public static class CharacterExtensions
{
    public static bool IsPrintable(this char self)
    {
        return !char.IsControl(self) && self != ' ';
    }

    public static ReadOnlySpan<TElement> InlineArrayAsReadOnlySpan<TBuffer, TElement>(this TBuffer buffer, int size)
        where TBuffer : struct
    {
        return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TBuffer, TElement>(ref Unsafe.AsRef(in buffer)), size);
    }
}