using System.Diagnostics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
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

    public unsafe void WriteRaw(in IntPtr src, ulong size, ulong offset = 0)
    {
        Debug.Assert(src != IntPtr.Zero);
        Debug.Assert(IsValid, "Buffer is not valid");
        IGraphicsModule.Get().WriteBuffer(Buffer, new ReadOnlySpan<byte>((void*)src, (int)size), Offset + offset);
    }

    public void Write<T>(T[] data, ulong offset = 0) where T : unmanaged
    {
        unsafe
        {
            fixed (T* pData = data)
            {
                WriteRaw(new IntPtr(pData), Utils.ByteSizeOf<T>(data.Length), offset);
            }
        }
    }
    
    public void Write<T>(Span<T> data, ulong offset = 0) where T : unmanaged
    {
        unsafe
        {
            fixed (T* pData = data)
            {
                WriteRaw(new IntPtr(pData), Utils.ByteSizeOf<T>(data.Length), offset);
            }
        }
    }
    
    public void Write<T>(Memory<T> data, ulong offset = 0) where T : unmanaged
    {
        unsafe
        {
            fixed (T* pData = data.Span)
            {
                WriteRaw(new IntPtr(pData), Utils.ByteSizeOf<T>(data.Length), offset);
            }
        }
    }

    public void Write<T>(ReadOnlySpan<T> data, ulong offset = 0) where T : unmanaged
    {
        unsafe
        {
            fixed (T* pData = data)
            {
                WriteRaw(new IntPtr(pData), Utils.ByteSizeOf<T>(data.Length), offset);
            }
        }
    }
    
    public void Write<T>(ReadOnlyMemory<T> data, ulong offset = 0) where T : unmanaged
    {
        unsafe
        {
            fixed (T* pData = data.Span)
            {
                WriteRaw(new IntPtr(pData), Utils.ByteSizeOf<T>(data.Length), offset);
            }
        }
    }
    
    public void Write<T>(T src, ulong offset = 0) where T : unmanaged
    {
        unsafe
        {
            WriteRaw(new IntPtr(&src), Utils.ByteSizeOf<T>(), offset);
        }
    }

    public void Write<T>(IReadOnlyBuffer<T> src, ulong offset = 0) where T : unmanaged
    {
        Debug.Assert(src.GetPtr() != IntPtr.Zero, "src buffer is null");
        Debug.Assert(src.ElementCount > 0, "src buffer is empty");
        WriteRaw(src.GetPtr(), src.ByteSize, offset);
    }

    public DeviceBufferView GetView(ulong offset, ulong size)
    {
        var newOffset = Offset + offset;
        //Debug.Assert(IsValid,"Buffer is not valid");
        Debug.Assert(newOffset <= Offset + Size, "Offset out of range");
        Debug.Assert((newOffset + size) <= (Offset + Size), "Offset out of range");
        return new DeviceBufferView(Buffer, newOffset, size);
    }
    
    public DeviceBufferView GetView<T>(ulong offset) where T : unmanaged
    {
        return GetView(offset, Utils.ByteSizeOf<T>());
    }

    public ulong GetAddress()
    {
        Debug.Assert(IsValid, "Buffer is not valid");
        return IGraphicsModule.Get().GetBufferAddress(Buffer) + Offset;
    }
}