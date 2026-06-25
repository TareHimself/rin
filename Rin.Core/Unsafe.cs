namespace Rin.Core;

public static class Memory
{
    // public static partial class Memory
    // {
    //     [LibraryImport(DllName, EntryPoint = "memoryAllocate")]
    //     [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    //     public static partial IntPtr Allocate(ulong size);
    //
    //     [LibraryImport(DllName, EntryPoint = "memorySet")]
    //     [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    //     public static partial void Set(IntPtr ptr, int value, ulong size);
    //
    //     [LibraryImport(DllName, EntryPoint = "memoryFree")]
    //     [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    //     public static partial void Free(IntPtr ptr);
    // }
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