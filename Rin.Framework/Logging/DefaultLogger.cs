namespace Rin.Framework.Logging;

public sealed class DefaultLogger : ILogger
{
    private readonly Func<string, string, string> _formatter = (_, m) => m;
    private readonly List<ITransport> _transports = [];

    public DefaultLogger()
    {
    }

    public DefaultLogger(Func<string, string, string> formatter)
    {
        _formatter = formatter;
    }

    public string FormatMessage(string severity, string message)
    {
        return _formatter(severity, message);
    }

    public IEnumerable<ITransport> GetTransports()
    {
        return _transports;
    }

    public ILogger AddTransport(ITransport transport)
    {
        _transports.Add(transport);
        transport.OnAdded(this);
        return this;
    }

    public void Dispose()
    {
        OnDispose(true);
        GC.SuppressFinalize(this);
    }

    private void OnDispose(bool manual)
    {
        foreach (var transport in GetTransports()) transport.Dispose();
    }

    ~DefaultLogger()
    {
        OnDispose(false);
    }
}