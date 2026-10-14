using Rin.Core.Shared.Logging;

namespace Examples.Common;

/// <summary>
///     The examples' log: one engine <see cref="ILogger" /> writing timestamped lines to the console, which
///     <c>--log-file</c> redirects to a file, with a source name on every line.
/// </summary>
public sealed class Log
{
    private static readonly ILogger Logger = new DefaultLogger(Format).AddConsole();
    private readonly string _source;

    private Log(string source)
    {
        _source = source;
    }

    public static Log For(string source)
    {
        return new Log(source);
    }

    public static Log For<T>()
    {
        return new Log(typeof(T).Name);
    }

    public void Info(string message)
    {
        Logger.Info($"{_source}: {message}");
    }

    public void Warn(string message)
    {
        Logger.Warn($"{_source}: {message}");
    }

    public void Error(string message, Exception? exception = null)
    {
        Logger.Error($"{_source}: {message}", exception);
    }

    private static string Format(string severity, string message)
    {
        return $"{DateTime.Now:HH:mm:ss.fff} {severity,-5} {message}";
    }
}
