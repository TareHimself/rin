using System.Diagnostics;
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
}