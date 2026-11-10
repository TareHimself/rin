using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Rin.Shade;

namespace Rin.Core.Graphics;

/// <summary>
///     A shader's view of buffer memory as an array of <typeparamref name="T" />: the buffer's bindless slot and a
///     byte offset into it. Elements are read and written whole, through <see cref="BindlessData.Buffers" />.
/// </summary>
[ShadeExport]
[StructLayout(LayoutKind.Sequential)]
public readonly struct DeviceSpan<T> where T : unmanaged
{
    public readonly uint Handle;
    public readonly uint Offset;

    public DeviceSpan(uint handle, uint offset)
    {
        Handle = handle;
        Offset = offset;
    }

    public T this[uint index]
    {
        get => BindlessData.GetBuffer(Handle).Load<T>(ByteOffset(index));
        set => BindlessData.GetBuffer(Handle).Store(ByteOffset(index), value);
    }

    public T this[int index]
    {
        get => this[(uint)index];
        set => this[(uint)index] = value;
    }

    /// <summary>
    ///     The first element.
    /// </summary>
    public T Value
    {
        get => this[0u];
        set => this[0u] = value;
    }

    /// <summary>
    ///     The byte offset of an element in the buffer.
    /// </summary>
    public uint ByteOffset(uint index)
    {
        return Offset + index * (uint)Unsafe.SizeOf<T>();
    }
}

/// <summary>
///     Atomics on a <see cref="DeviceSpan{T}" /> of uints. Each returns the element's prior value.
/// </summary>
[ShadeExport]
public static class DeviceSpanAtomics
{
    public static uint InterlockedAdd(this DeviceSpan<uint> span, uint index, uint value)
    {
        BindlessData.GetBuffer(span.Handle).InterlockedAdd(span.ByteOffset(index), value, out var original);
        return original;
    }

    public static uint InterlockedMin(this DeviceSpan<uint> span, uint index, uint value)
    {
        BindlessData.GetBuffer(span.Handle).InterlockedMin(span.ByteOffset(index), value, out var original);
        return original;
    }

    public static uint InterlockedMax(this DeviceSpan<uint> span, uint index, uint value)
    {
        BindlessData.GetBuffer(span.Handle).InterlockedMax(span.ByteOffset(index), value, out var original);
        return original;
    }

    public static uint InterlockedAnd(this DeviceSpan<uint> span, uint index, uint value)
    {
        BindlessData.GetBuffer(span.Handle).InterlockedAnd(span.ByteOffset(index), value, out var original);
        return original;
    }

    public static uint InterlockedOr(this DeviceSpan<uint> span, uint index, uint value)
    {
        BindlessData.GetBuffer(span.Handle).InterlockedOr(span.ByteOffset(index), value, out var original);
        return original;
    }

    public static uint InterlockedXor(this DeviceSpan<uint> span, uint index, uint value)
    {
        BindlessData.GetBuffer(span.Handle).InterlockedXor(span.ByteOffset(index), value, out var original);
        return original;
    }

    public static uint InterlockedExchange(this DeviceSpan<uint> span, uint index, uint value)
    {
        BindlessData.GetBuffer(span.Handle).InterlockedExchange(span.ByteOffset(index), value, out var original);
        return original;
    }

    public static uint InterlockedCompareExchange(this DeviceSpan<uint> span, uint index, uint compare, uint value)
    {
        BindlessData.GetBuffer(span.Handle)
            .InterlockedCompareExchange(span.ByteOffset(index), compare, value, out var original);
        return original;
    }
}
