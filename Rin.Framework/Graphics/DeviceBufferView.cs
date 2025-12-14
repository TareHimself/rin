using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Rin.Framework.Buffers;

namespace Rin.Framework.Graphics;

public readonly record struct DeviceBufferView
{
    public DeviceBufferView()
    {
        Buffer = null;
        Offset = 0;
        Size = 0;
    }

    public DeviceBufferView(IDeviceBuffer? buffer, ulong inOffset, ulong inSize)
    {
        Buffer = buffer;
        Offset = inOffset;
        Size = inSize;
    }

    public IDeviceBuffer? Buffer { get; }

    [PublicAPI] public ulong Offset { get; }

    [PublicAPI] public ulong Size { get; }

    [PublicAPI]
    [MemberNotNullWhen(true, nameof(Buffer))]
    public bool IsValid => Buffer != null;
    
    public void WriteRaw(in IntPtr src, ulong size, ulong offset = 0)
    {
        Debug.Assert(src != IntPtr.Zero);
        Debug.Assert(IsValid, "Buffer is not valid");
        Buffer.WriteRaw(src, size, Offset + offset);
    }

    public void Write<T>(IEnumerable<T> data, ulong offset = 0) where T : unmanaged
    {
        Debug.Assert(IsValid, "Buffer is not valid");
        Write(data.ToArray(),offset);
    }

    public void Write<T>(T[] data, ulong offset = 0) where T : unmanaged
    {
        unsafe
        {
            fixed (T* pData = data)
            {
                WriteRaw(new IntPtr(pData),Utils.ByteSizeOf<T>(data.Length), offset);
            }
        }
    }

    public void Write<T>(List<T> data, ulong offset = 0) where T : unmanaged
    {
        unsafe
        {
            fixed (T* pData = CollectionsMarshal.AsSpan(data))
            {
                WriteRaw(new IntPtr(pData),Utils.ByteSizeOf<T>(data.Count), offset);
            }
        }
    }

   

    public void Write<T>(T src, ulong offset = 0) where T : unmanaged
    {
        unsafe
        {
            WriteRaw(new IntPtr(&src), Utils.ByteSizeOf<T>(),offset);
        }
    }
    
    public void Write<T>(IReadOnlyBuffer<T> src, ulong offset = 0) where T : unmanaged
    {
        Debug.Assert(src.GetPtr() != IntPtr.Zero, "src buffer is null");
        Debug.Assert(src.ElementCount > 0, "src buffer is empty");
        WriteRaw(src.GetPtr(),src.ByteSize,offset);
    }

    public DeviceBufferView GetView(ulong offset, ulong size)
    {
        var newOffset = Offset + offset;
        //Debug.Assert(IsValid,"Buffer is not valid");
        Debug.Assert(newOffset <= Offset + Size, "Offset out of range");
        return new DeviceBufferView(Buffer, Offset + offset, size);
    }

    public ulong GetAddress()
    {
        Debug.Assert(IsValid, "Buffer is not valid");
        return Buffer.GetAddress() + Offset;
    }
}