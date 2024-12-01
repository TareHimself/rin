namespace rin.Framework.Core.Logging;

public interface ITransport : IDisposable
{
    abstract void OnAdded(ILogger logger);
    abstract void Info(string message);
    abstract void Warn(string message);
    abstract void Error(string message,Exception? exception);
}