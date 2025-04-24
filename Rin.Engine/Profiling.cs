#define RIN_PROFILING

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Rin.Engine;

public abstract class Profiling : IStaticProfiler
{
    #if RIN_PROFILING
    class ProfilingInfo
    {
        public readonly Stopwatch Watch = new Stopwatch();
        public TimeSpan LastElapsedTime = TimeSpan.Zero;
    }

    private static readonly ConcurrentDictionary<string, ProfilingInfo> ProfilingInfos = [];
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Begin(string id)
    {
        ProfilingInfo? info;
        {
            ProfilingInfos.TryGetValue(id, out info);
        }

        if (info == null)
        {
            info = new ProfilingInfo();
            ProfilingInfos.AddOrUpdate(id,(_) => info,(_,__) => info);
        }
        
        info.Watch.Restart();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void End(string id)
    {
        if (!ProfilingInfos.TryGetValue(id, out var info))
        {
            throw new Exception($"Begin was never called for {id}");
        }
        
        info.Watch.Stop();
        info.LastElapsedTime = info.Watch.Elapsed;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Measure(string id,Action? action)
    {
        if (action == null) return;
        Begin(id);
        action.Invoke();
        End(id);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Measure<T>(string id,Func<T> action)
    {
        Begin(id);
        var result = action();
        End(id);
        return result;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimeSpan GetElapsed(string id)
    {
        if (!ProfilingInfos.TryGetValue(id, out var info))
        {
            throw new Exception($"no profiling info found for {id}");
        }

        return info.LastElapsedTime;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimeSpan GetElapsedOrZero(string id)
    {
        if (!ProfilingInfos.TryGetValue(id, out var info))
        {
            return TimeSpan.Zero;
        }

        return info.LastElapsedTime;
    }

    public static string[] GetIds() => ProfilingInfos.Keys.ToArray();
    #else
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Begin(string id)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void End(string id)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Measure(string id, Action? action)
    {
        action?.Invoke();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Measure<T>(string id, Func<T> action)
    {
        return action();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimeSpan GetElapsed(string id)
    {
        throw new NotImplementedException();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimeSpan GetElapsedOrZero(string id)
    {
        return TimeSpan.Zero;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string[] GetIds() => [];
#endif
}