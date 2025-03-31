using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Rin.Engine.Core;

public static class Utils
{
    public static void Measure(string name, Action func)
    {
        var watch = Stopwatch.StartNew();
        func();
        watch.Stop();
        Console.WriteLine($"[Measure: {name}]: {watch.Elapsed.Microseconds / 1000.0f}ms");
    }
    
    public static void Benchmark(string name, Action func, int iterations)
    {
        var watch = Stopwatch.StartNew();
        watch.Stop();
        var total = TimeSpan.Zero;
        for (var i = 0; i < iterations; i++)
        {
            watch.Restart();
            func();
            watch.Stop();
            total += watch.Elapsed;
        }
        
        Console.WriteLine($"[Benchmark: {name}]: {(total.TotalNanoseconds / iterations) / 1000.0f}ms");
    }

    public static T Measure<T>(string name, Func<T> func)
    {
        var watch = Stopwatch.StartNew();
        var r = func();
        watch.Stop();
        Console.WriteLine($"[Measure: {name}]: {watch.Elapsed.Microseconds / 1000.0f}ms");
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