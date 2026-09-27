using System.Runtime.InteropServices;

namespace Rin.Native.Fake;

public static unsafe class NativeFake
{
    [UnmanagedCallersOnly(EntryPoint = "memoryAllocate")]
    public static void* MemoryAllocate(ulong size) => NativeMemory.Alloc((nuint)size);

    [UnmanagedCallersOnly(EntryPoint = "memorySet")]
    public static void MemorySet(void* ptr, int value, ulong size) =>
        new Span<byte>(ptr, (int)size).Fill((byte)value);

    [UnmanagedCallersOnly(EntryPoint = "memoryFree")]
    public static void MemoryFree(void* ptr) => NativeMemory.Free(ptr);
}
