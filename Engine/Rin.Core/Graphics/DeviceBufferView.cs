using System.Diagnostics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Rin.Core.Shared;
using Rin.Core.Shared.Buffers;

namespace Rin.Core.Graphics;

public readonly record struct DeviceBufferView
{
    public DeviceBufferView()
    {
        Buffer = ResourceHandle.InvalidBuffer;
        Offset = 0;
        Size = 0;
    }

    public DeviceBufferView(ResourceHandle buffer, ulong inOffset, ulong inSize)
    {
        Buffer = buffer;
        Offset = inOffset;
        Size = inSize;
    }

    public ResourceHandle Buffer { get; }

    [PublicAPI] public ulong Offset { get; }

    [PublicAPI] public ulong Size { get; }

    [PublicAPI]
    public bool IsValid => Buffer.Id != 0;

    public unsafe ulong WriteRaw(in IntPtr src, ulong size, ulong offset = 0)
    {
        return WriteBytes(new ReadOnlySpan<byte>((void*)src, (int)size), offset);
    }

    public ulong Write<T>(T[] data, ulong offset = 0) where T : unmanaged => Write(new ReadOnlySpan<T>(data), offset);

    public ulong Write<T>(PooledMemory<T> data, ulong offset = 0) where T : unmanaged => Write(data.AsSpan(), offset);

    public ulong Write<T>(List<T> data, ulong offset = 0) where T : unmanaged =>
        Write(CollectionsMarshal.AsSpan(data), offset);

    public ulong Write<T>(Memory<T> data, ulong offset = 0) where T : unmanaged => Write(data.Span, offset);

    public ulong Write<T>(ReadOnlyMemory<T> data, ulong offset = 0) where T : unmanaged => Write(data.Span, offset);

    public ulong Write<T>(ReadOnlySpan<T> data, ulong offset = 0) where T : unmanaged =>
        WriteBytes(MemoryMarshal.AsBytes(data), offset);

    public ulong WriteSingle<T>(T src, ulong offset = 0) where T : unmanaged =>
        Write(new ReadOnlySpan<T>(in src), offset);

    public ulong Write<T>(IReadOnlyBuffer<T> src, ulong offset = 0) where T : unmanaged
    {
        Debug.Assert(src.GetPtr() != IntPtr.Zero, "src buffer is null");
        Debug.Assert(src.ElementCount > 0, "src buffer is empty");
        return WriteRaw(src.GetPtr(), src.ByteSize, offset);
    }

    private ulong WriteBytes(ReadOnlySpan<byte> bytes, ulong offset)
    {
        if (bytes.IsEmpty) return 0;
        Debug.Assert(IsValid, "Buffer is not valid");
        IGraphicsModule.Get().WriteBuffer(Buffer, bytes, Offset + offset);
        return (ulong)bytes.Length;
    }

    public DeviceBufferView GetView(ulong offset, ulong size)
    {
        var newOffset = Offset + offset;
        Debug.Assert(newOffset <= Offset + Size, "Offset out of range");
        Debug.Assert((newOffset + size) <= (Offset + Size), "Offset out of range");
        return new DeviceBufferView(Buffer, newOffset, size);
    }
    
    public DeviceBufferView GetView<T>(ulong offset) where T : unmanaged
    {
        return GetView(offset, Utils.ByteSizeOf<T>());
    }

    /// <summary>
    ///     A shader's view of this range: the buffer's bindless slot and the range's byte offset.
    /// </summary>
    public DeviceSpan<T> ToDeviceSpan<T>() where T : unmanaged
    {
        Debug.Assert(IsValid, "Buffer is not valid");
        Debug.Assert(Offset % 4 == 0, "Shaders read buffers at 4-byte aligned offsets");
        Debug.Assert(Offset <= uint.MaxValue, "A buffer offset must fit in 32 bits");
        return new DeviceSpan<T>(Buffer.Id, (uint)Offset);
    }
}
