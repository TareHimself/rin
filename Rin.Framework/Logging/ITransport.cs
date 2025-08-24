namespace Rin.Framework.Logging;

public interface ITransport : IDisposable
{
    void OnAdded(ILogger logger);
    void Info(string message);
    void Warn(string message);
    void Error(string message, Exception? exception);
}