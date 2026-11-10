using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Rin.Shade.Tests;

/// <summary>
///     A bindless block of buffers for test shaders, standing in for the engine's. Every test compilation includes
///     this file, so test sources reach buffers the way engine shaders do.
/// </summary>
[BindlessBlock(Name)]
public struct TestHeap
{
    public const string Name = "test.heap";
    public const int BufferCount = 8;

    public static TestHeapBuffers Buffers;
}

[InlineArray(TestHeap.BufferCount)]
public struct TestHeapBuffers
{
    private RWByteAddressBuffer _element;
}

/// <summary>
///     A test shader's view of a buffer in <see cref="TestHeap" /> as an array of <typeparamref name="T" />.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct TestBuffer<T> where T : unmanaged
{
    public readonly uint Handle;
    public readonly uint Offset;

    public TestBuffer(uint handle, uint offset)
    {
        Handle = handle;
        Offset = offset;
    }

    public T this[uint index]
    {
        get => TestHeap.Buffers[(int)Handle].Load<T>(ByteOffset(index));
        set => TestHeap.Buffers[(int)Handle].Store(ByteOffset(index), value);
    }

    public T this[int index]
    {
        get => this[(uint)index];
        set => this[(uint)index] = value;
    }

    public uint ByteOffset(uint index)
    {
        return Offset + index * (uint)Unsafe.SizeOf<T>();
    }
}
