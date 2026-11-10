using System.Runtime.InteropServices;
using Rin.Shade.Tests;
using Rin.Slang.Compiler;

namespace Rin.Shade.CpuTests;

/// <summary>
///     Pins managed arrays as a host-callable shader's <see cref="TestHeap" />, where each array's handle is its index.
/// </summary>
internal sealed unsafe class PinnedBuffers : IDisposable
{
    private readonly HostBuffer[] _buffers = new HostBuffer[TestHeap.BufferCount];
    private readonly List<GCHandle> _pins = [];

    public ReadOnlySpan<HostBuffer> Heap => _buffers;

    public void Dispose()
    {
        foreach (var pin in _pins) pin.Free();
    }

    public TestBuffer<T> Add<T>(T[] array) where T : unmanaged
    {
        var pin = GCHandle.Alloc(array, GCHandleType.Pinned);
        _buffers[_pins.Count] = new HostBuffer((void*)pin.AddrOfPinnedObject(), (nuint)(array.Length * sizeof(T)));
        _pins.Add(pin);
        return new TestBuffer<T>((uint)(_pins.Count - 1), 0);
    }
}
