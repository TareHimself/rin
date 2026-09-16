namespace Rin.Core;

public static class Memory
{
    public static IntPtr Allocate(ulong size)
    {
        return Native.memoryAllocate(size);
    }

    public static void Set(IntPtr ptr, int value, ulong size)
    {
        Native.memorySet(ptr, value, size);
    }
    public static void Free(IntPtr ptr)
    {
        Native.memoryFree(ptr);
    }
}