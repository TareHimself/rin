using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Rin.Engine.Core;

public static class Utils
{
    public static void MeasureSync(string name, Action func)
    {
        var watch = Stopwatch.StartNew();
        func();
        watch.Stop();
        Console.WriteLine($"[MeasureSync]: {name} => {watch.Elapsed.Microseconds / 1000.0f}ms");
    }

    public static T MeasureSync<T>(string name, Func<T> func)
    {
        var watch = Stopwatch.StartNew();
        var r = func();
        watch.Stop();
        Console.WriteLine($"[MeasureSync]: {name} => {watch.Elapsed.Microseconds / 1000.0f}ms");
        return r;
    }

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
    
    /// <summary>
    ///     Performns no bounds checking, assumes sizeof(dest) >= sizeof(source)
    /// </summary>
    /// <param name="src"></param>
    /// <typeparam name="TDest"></typeparam>
    /// <typeparam name="TSource"></typeparam>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TDest* Reinterpret<TDest, TSource>(TSource* src)
        where TDest : unmanaged where TSource : unmanaged
    {
        return (TDest*)src;
    }
}