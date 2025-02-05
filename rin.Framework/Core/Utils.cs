using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace rin.Framework.Core;

public static class Utils
{
    public static void MeasureSync(string name,Action func)
    {
        var watch = Stopwatch.StartNew();
        func();
        watch.Stop();
        Console.WriteLine($"[MeasureSync]: {name} => {watch.Elapsed.Microseconds / 1000.0f}ms");
    }
    public static T MeasureSync<T>(string name,Func<T> func)
    {
        var watch = Stopwatch.StartNew();
        var r = func();
        watch.Stop();
        Console.WriteLine($"[MeasureSync]: {name} => {watch.Elapsed.Microseconds / 1000.0f}ms");
        return r;
    }

    public static ulong ByteSizeOf<T>(int count = 1) where T : unmanaged
    {
        unsafe
        {
            var size = (ulong)sizeof(T);
            return size * (ulong)count;
        }
    }

    
    /// <summary>
    /// Performns no bounds checking, assumes sizeof(dest) >= sizeof(source)
    /// </summary>
    /// <param name="src"></param>
    /// <typeparam name="TDest"></typeparam>
    /// <typeparam name="TSource"></typeparam>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TDest* Reinterpret<TDest, TSource>(TSource* src)
        where TDest : unmanaged where TSource : unmanaged => (TDest*)src;

}