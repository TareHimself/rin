using System.Collections.Concurrent;
using System.Diagnostics;

namespace Rin.Engine;

public static class Profiling
{
    class ProfilingInfo
    {
        public readonly Stopwatch Watch = new Stopwatch();
        public TimeSpan LastElapsedTime = TimeSpan.Zero;
    }

    private static readonly ConcurrentDictionary<string, ProfilingInfo> ProfilingInfos = [];


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

    public static void End(string id)
    {
        if (!ProfilingInfos.TryGetValue(id, out var info))
        {
            throw new Exception($"Begin was never called for {id}");
        }
        
        info.Watch.Stop();
        info.LastElapsedTime = info.Watch.Elapsed;
    }
    
    public static TimeSpan GetElapsed(string id)
    {
        if (!ProfilingInfos.TryGetValue(id, out var info))
        {
            throw new Exception($"no profiling info found for {id}");
        }

        return info.LastElapsedTime;
    }
    
    public static TimeSpan GetElapsedOrZero(string id)
    {
        if (!ProfilingInfos.TryGetValue(id, out var info))
        {
            return TimeSpan.Zero;
        }

        return info.LastElapsedTime;
    }

    public static string[] GetIds() => ProfilingInfos.Keys.ToArray();
}