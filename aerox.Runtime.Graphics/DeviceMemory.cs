using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace aerox.Runtime.Graphics;

/// <summary>
///     GPU Memory
/// </summary>
public abstract partial class DeviceMemory : MultiDisposable
{
    public readonly IntPtr Allocation;
    protected readonly Allocator Allocator;

    protected DeviceMemory(Allocator inAllocator, IntPtr inAllocation)
    {
        Allocator = inAllocator;
        Allocation = inAllocation;
    }

    [LibraryImport(Runtime.Dlls.AeroxNative, EntryPoint = "graphicsAllocatorCopyToBuffer")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe partial void NativeCopyToBuffer(IntPtr allocator, void* allocation, void* data, ulong size,
        ulong offset);

    public unsafe void Write(void* src, ulong size, ulong offset = 0)
    {
        NativeCopyToBuffer(Allocator, Allocation.ToPointer(), src, size, offset);
    }

    public void Write<T>(T[] data, ulong offset = 0)
    {
        unsafe
        {
            fixed (T* pData = data)
            {
                Write(pData, (ulong)(data.Length * Marshal.SizeOf<T>()), offset);
            }
        }
    }

    public void Write<T>(T src, ulong offset = 0)
    {
        unsafe
        {
            Write(&src, (ulong)Marshal.SizeOf<T>(), offset);
        }
    }
}