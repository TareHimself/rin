namespace Rin.Core;

public static class Utils
{
    public static ulong ByteSizeOf<T>() where T : unmanaged
    {
        unsafe
        {
            return (ulong)sizeof(T);
        }
    }

    public static ulong ByteSizeOf<T>(int count) where T : unmanaged
    {
        unsafe
        {
            return (ulong)sizeof(T) * (ulong)count;
        }
    }
}