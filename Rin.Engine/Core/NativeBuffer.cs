using System.Runtime.InteropServices;
using Rin.Engine.Core.Extensions;

namespace Rin.Engine.Core;

public class NativeBuffer<T> : IDisposable, IBinarySerializable, ICloneable<NativeBuffer<T>>
    where T : unmanaged
{
    private IntPtr _ptr = IntPtr.Zero;
    private int _elements = 0;

    public NativeBuffer(int elements)
    {
        if (elements <= 0) return;
        
        _elements = elements;
        
        _ptr = Marshal.AllocHGlobal((int)Utils.ByteSizeOf<T>(elements));
    }
    
    public NativeBuffer(ReadOnlySpan<T> elements)
    {
        var elementCount = elements.Length;
        
        if (elementCount <= 0) return;
        
        _elements = elementCount;
        
        _ptr = Marshal.AllocHGlobal((int)Utils.ByteSizeOf<T>(elementCount));
        
        unsafe
        {
            elements.CopyTo(new Span<T>(_ptr.ToPointer(), elementCount));
        }
    }
    
    public NativeBuffer() : this(0)
    {
    }
    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    public unsafe T* GetData()
    {
        return (T*)_ptr;
    }

    public IntPtr GetPtr()
    {
        return _ptr;
    }

    public int GetElementsCount()
    {
        return _elements;
    }
    
    public int GetElementByteSize()
    {
        return Marshal.SizeOf<T>();
    }

    public int GetByteSize()
    {
        return _elements * GetElementByteSize();
    }

    public static implicit operator Span<T>(NativeBuffer<T> buff) => buff.AsSpan();
    
    public static implicit operator ReadOnlySpan<T>(NativeBuffer<T> buff) => buff.AsReadOnlySpan();

    private void ReleaseUnmanagedResources()
    {
        if (_ptr != IntPtr.Zero) Marshal.FreeHGlobal(_ptr);
    }

    public unsafe void Write(IntPtr src, uint size)
    {
        Buffer.MemoryCopy(src.ToPointer(), GetPtr().ToPointer(), GetByteSize(), size);
    }

    public Span<T> AsSpan()
    {
        unsafe
        {
            return new Span<T>(GetPtr().ToPointer(), GetElementsCount());
        }
    }

    public ReadOnlySpan<T> AsReadOnlySpan()
    {
        unsafe
        {
            return new ReadOnlySpan<T>(GetPtr().ToPointer(), GetElementsCount());
        }
    }

    public NativeBuffer<T> Clone()
    {
        var buff = new NativeBuffer<T>(_elements);
        buff.Write(GetPtr(), (uint)GetElementByteSize());
        return buff;
    }

    ~NativeBuffer()
    {
        ReleaseUnmanagedResources();
    }

    public void BinarySerialize(Stream output)
    {
        unsafe
        {
            var byteSize = GetByteSize();
            output.Write(byteSize);
            output.Write(new ReadOnlySpan<byte>(_ptr.ToPointer(),byteSize));
        }
    }

    public void BinaryDeserialize(Stream input)
    {
        unsafe
        {
            if (_ptr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_ptr);
                _ptr = IntPtr.Zero;
                _elements = 0;
            }
            var byteSize = input.ReadInt32();
            _ptr = Marshal.AllocHGlobal(byteSize);
            _elements = byteSize / GetElementByteSize();
            var read = input.Read(new Span<byte>(_ptr.ToPointer(), byteSize));
        }
    }
}