namespace Rin.Engine.Core.Logging;

public interface ILogger : IDisposable
{
    ILogger AddTransport(ITransport transport);

    IEnumerable<ITransport> GetTransports();

    ILogger Info(string message)
    {
        var formatted = FormatMessage("info", message);
        foreach (var transport in GetTransports()) transport.Info(formatted);

        return this;
    }

    ILogger Warn(string message)
    {
        var formatted = FormatMessage("warn", message);
        foreach (var transport in GetTransports()) transport.Warn(formatted);

        return this;
    }

    ILogger Error(string message, Exception? exception = null)
    {
        var formatted = FormatMessage("error", message);
        foreach (var transport in GetTransports()) transport.Error(formatted, exception);

        return this;
    }

    string FormatMessage(string severity, string message)
    {
        return message;
    }
}