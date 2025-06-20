using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Graphics;

public readonly record struct DeviceBufferView : IDeviceBufferWriteOps
{
    public IDeviceBuffer? Buffer { get; }
    
    [PublicAPI]
    public ulong Offset { get; }
    
    [PublicAPI]
    public ulong Size { get; }

    [PublicAPI,MemberNotNullWhen(true, nameof(Buffer))]
    public bool IsValid => Buffer != null;

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

    public DeviceBufferView GetView(ulong offset, ulong size)
    {
        var newOffset = Offset + offset;
        //Debug.Assert(IsValid,"Buffer is not valid");
        Debug.Assert(newOffset <= (Offset + Size), "Offset out of range");
        return new DeviceBufferView(Buffer,Offset + offset, size);
    }

    public ulong GetAddress()
    {
        Debug.Assert(IsValid,"Buffer is not valid");
        return Buffer.GetAddress() + Offset;
    }

    public unsafe void Write(void* src, ulong size, ulong offset = 0)
    {
        Debug.Assert(IsValid,"Buffer is not valid");
        Debug.Assert(Buffer.NativeBuffer.Value != 0);
        Buffer.Write(src, size,Offset + offset);
    }

    public void WriteArray<T>(IEnumerable<T> data, ulong offset = 0) where T : unmanaged
    {
        Debug.Assert(IsValid,"Buffer is not valid");
        Buffer.WriteArray(data,Offset + offset);
    }

    public void Write(IntPtr src, ulong size, ulong offset = 0)
    {
        Debug.Assert(IsValid,"Buffer is not valid");
        Debug.Assert(src != IntPtr.Zero);
        Buffer.Write(src,size,Offset + offset);
    }

    public void WriteStruct<T>(T src, ulong offset = 0) where T : unmanaged
    {
        Debug.Assert(IsValid,"Buffer is not valid");
        Buffer.WriteStruct(src,Offset + offset);
    }

    public void WriteBuffer<T>(Buffer<T> src, ulong offset = 0) where T : unmanaged
    {
        Debug.Assert(IsValid,"Buffer is not valid");
        Debug.Assert(src.GetPtr() != IntPtr.Zero, "src buffer is null");
        Debug.Assert(src.GetElementsCount() > 0, "src buffer is empty");
        Buffer.WriteBuffer(src,Offset + offset);
    }
    
}