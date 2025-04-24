namespace Rin.Engine;

public interface IStaticProfiler
{
    public static abstract void Begin(string id);

    public static abstract void End(string id);

    public static abstract void Measure(string id, Action? action);

    public static abstract T Measure<T>(string id, Func<T> action);

    public static abstract TimeSpan GetElapsed(string id);

    public static abstract  TimeSpan GetElapsedOrZero(string id);

    public static abstract  string[] GetIds();
}