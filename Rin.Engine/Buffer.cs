using System.Collections;
using System.Runtime.InteropServices;
using Rin.Engine.Extensions;

namespace Rin.Engine;

public class Buffer<T> : IDisposable, IBinarySerializable, ICopyable<Buffer<T>> //, IEnumerable<T>
    where T : unmanaged
{
    private int _elements;
    private IntPtr _ptr = IntPtr.Zero;

    public Buffer(int elements)
    {
        if (elements <= 0) return;

        _elements = elements;
        unsafe
        {
            _ptr = new IntPtr(Native.Memory.Allocate(Utils.ByteSizeOf<T>(elements)));
        }
    }

    public unsafe Buffer(T* data, int elements)
    {
        if (elements <= 0) return;

        _elements = elements;
        _ptr = new IntPtr(Native.Memory.Allocate(Utils.ByteSizeOf<T>(elements)));
        Write(data, elements);
    }

    public Buffer(ReadOnlySpan<T> elements)
    {
        var elementCount = elements.Length;

        if (elementCount <= 0) return;

        _elements = elementCount;
        unsafe
        {
            _ptr = new IntPtr(Native.Memory.Allocate(Utils.ByteSizeOf<T>(elementCount)));
            elements.CopyTo(new Span<T>(_ptr.ToPointer(), elementCount));
        }
    }

    public Buffer() : this(0)
    {
    }

    public void BinarySerialize(Stream output)
    {
        unsafe
        {
            var byteSize = GetByteSize();
            output.Write(byteSize);
            output.Write(new ReadOnlySpan<byte>(_ptr.ToPointer(), (int)byteSize));
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

            var byteSize = (nuint)input.ReadUInt64();
            _ptr = new IntPtr(Native.Memory.Allocate(byteSize));
            _elements = (int)(byteSize / GetElementByteSize());
            var read = input.Read(new Span<byte>(_ptr.ToPointer(), (int)byteSize));
        }
    }

    public Buffer<T> Copy()
    {
        var buff = new Buffer<T>(_elements);
        buff.Write(AsReadOnlySpan());
        return buff;
    }

    // public IEnumerator<T> GetEnumerator()
    // {
    //     return new Enumerator(this);
    // }

    // IEnumerator IEnumerable.GetEnumerator()
    // {
    //     return GetEnumerator();
    // }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    public void Zero()
    {
        unsafe
        {
            Native.Memory.Set((void*)_ptr, 0, GetByteSize());
        }
    }


    public T GetElement(int index)
    {
        unsafe
        {
            return *((T*)_ptr + index);
        }
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

    public ulong GetElementByteSize()
    {
        return Utils.ByteSizeOf<T>();
    }

    public ulong GetByteSize()
    {
        return (ulong)_elements * GetElementByteSize();
    }

    public static implicit operator Span<T>(Buffer<T> buff)
    {
        return buff.AsSpan();
    }

    public static implicit operator ReadOnlySpan<T>(Buffer<T> buff)
    {
        return buff.AsReadOnlySpan();
    }


    private void ReleaseUnmanagedResources()
    {
        unsafe
        {
            if (_ptr != IntPtr.Zero) Native.Memory.Free(_ptr.ToPointer());
        }

        _ptr = IntPtr.Zero;
    }

    public unsafe void Write(void* src, ulong size, ulong offset = 0)
    {
        Buffer.MemoryCopy(src, (void*)((nuint)GetPtr().ToPointer() + offset), GetByteSize(), size);
    }

    public unsafe void Write<TE>(TE* src, int numElements, ulong offset = 0) where TE : unmanaged
    {
        Buffer.MemoryCopy(src, (void*)((nuint)GetPtr().ToPointer() + offset), GetByteSize(),
            (ulong)numElements * Utils.ByteSizeOf<TE>());
    }

    public unsafe void Write(IntPtr src, ulong size, ulong offset = 0)
    {
        Buffer.MemoryCopy(src.ToPointer(), (void*)((nuint)GetPtr().ToPointer() + offset), GetByteSize(), size);
    }

    public unsafe void Write(ReadOnlySpan<T> data, ulong offset = 0)
    {
        data.CopyTo(new Span<T>((void*)((nuint)GetPtr().ToPointer() + offset), data.Length));
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

    ~Buffer()
    {
        ReleaseUnmanagedResources();
    }

    public class Enumerator(Buffer<T> buffer) : IEnumerator<T>
    {
        private int _index;

        public bool MoveNext()
        {
            if (_index + 1 >= buffer.GetElementsCount()) return false;

            ++_index;

            return true;
        }

        public void Reset()
        {
            _index = -1;
        }

        public T Current => buffer.GetElement(_index);

        object? IEnumerator.Current => Current;

        public void Dispose()
        {
        }
    }
}